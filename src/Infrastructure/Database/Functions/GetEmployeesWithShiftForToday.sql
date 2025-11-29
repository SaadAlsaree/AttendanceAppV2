-- ====================================================================
-- PostgreSQL Function: Get Employees With Shift For Today
-- ====================================================================
-- Description: Returns list of employees with their shift information for today
-- Author: System
-- Date: 2025-11-14
-- ====================================================================

CREATE OR REPLACE FUNCTION public.get_employees_with_shift_for_today()
RETURNS TABLE (
    employee_id UUID,
    employee_name VARCHAR,
    organization_id UUID,
    shift_id UUID,
    shift_name VARCHAR,
    shift_source VARCHAR,
    attendance_schedule_id UUID,
    has_schedule BOOLEAN,
    has_schedule_day BOOLEAN,
    has_exception BOOLEAN
) 
LANGUAGE plpgsql
AS $$
DECLARE
    v_today DATE;
BEGIN
    -- Get today's date
    v_today := CURRENT_DATE;

    RETURN QUERY
    WITH employee_schedules AS (
        -- Get active schedules for employees
        SELECT DISTINCT ON (asch.employee_id)
            asch.id AS schedule_id,
            asch.employee_id,
            asch.excluded_dates
        FROM public."AttendanceSchedules" asch
        WHERE 
            asch.is_active = true
            AND asch.is_deleted = false
            AND asch.start_date <= v_today
            AND (asch.end_date IS NULL OR asch.end_date >= v_today)
            -- Exclude schedules that explicitly exclude today
            AND (
                asch.excluded_dates IS NULL 
                OR asch.excluded_dates = ''
                OR NOT (v_today::text = ANY(string_to_array(asch.excluded_dates, ',')))
            )
        ORDER BY asch.employee_id, asch.created_at DESC
    ),
    schedule_exceptions AS (
        -- Get exceptions (ScheduleIssues) for today
        SELECT DISTINCT ON (es.employee_id)
            es.employee_id,
            si.shift_id,
            si.id AS exception_id
        FROM employee_schedules es
        INNER JOIN public."ScheduleIssues" si ON si.attendance_schedule_id = es.schedule_id
        WHERE 
            si.date = v_today
            AND si.is_deleted = false
        ORDER BY es.employee_id, si.created_at DESC
    ),
    schedule_days AS (
        -- Get ScheduleDays for today
        SELECT DISTINCT ON (es.employee_id)
            es.employee_id,
            sd.shift_id,
            sd.id AS schedule_day_id
        FROM employee_schedules es
        INNER JOIN public."ScheduleDays" sd ON sd.attendance_schedule_id = es.schedule_id
        WHERE 
            sd.schedule_day_date = v_today
            AND sd.is_active = true
            AND sd.is_deleted = false
        ORDER BY es.employee_id, sd.created_at DESC
    )
    SELECT 
        e.id AS employee_id,
        e.full_name AS employee_name,
        e.organizational_unit_id AS organization_id,
        -- Get shift_id: Priority 1 - Exception, Priority 2 - ScheduleDay, Priority 3 - Employee's default shift
        COALESCE(
            se.shift_id,      -- Priority 1: From ScheduleIssue (exception)
            sd.shift_id,      -- Priority 2: From ScheduleDay
            e.shift_id        -- Priority 3: Employee's default shift
        ) AS shift_id,
        -- Get shift name
        COALESCE(
            (SELECT s1.name FROM public."Shifts" s1 WHERE s1.id = se.shift_id),
            (SELECT s2.name FROM public."Shifts" s2 WHERE s2.id = sd.shift_id),
            (SELECT s3.name FROM public."Shifts" s3 WHERE s3.id = e.shift_id),
            'No Shift'::VARCHAR
        ) AS shift_name,
        -- Indicate the source of shift_id
        (CASE 
            WHEN se.shift_id IS NOT NULL THEN 'Exception (ScheduleIssue)'
            WHEN sd.shift_id IS NOT NULL THEN 'ScheduleDay'
            WHEN e.shift_id IS NOT NULL THEN 'Employee Default'
            ELSE 'NULL'
        END)::VARCHAR AS shift_source,
        es.schedule_id AS attendance_schedule_id,
        (es.schedule_id IS NOT NULL) AS has_schedule,
        (sd.schedule_day_id IS NOT NULL) AS has_schedule_day,
        (se.exception_id IS NOT NULL) AS has_exception
    FROM 
        public."Employees" e
    LEFT JOIN employee_schedules es ON es.employee_id = e.id
    LEFT JOIN schedule_exceptions se ON se.employee_id = e.id
    LEFT JOIN schedule_days sd ON sd.employee_id = e.id
    WHERE 
        -- Employee must have an organizational unit
        e.organizational_unit_id IS NOT NULL
        -- Employee must not be deleted
        AND e.is_deleted = false
    ORDER BY 
        e.full_name;
END;
$$;

-- ====================================================================
-- Grant execute permission (adjust role as needed)
-- ====================================================================
-- GRANT EXECUTE ON FUNCTION public.get_employees_with_shift_for_today() TO your_app_user;

-- ====================================================================
-- Usage Examples:
-- ====================================================================
-- Basic usage:
-- SELECT * FROM public.get_employees_with_shift_for_today();

-- Get only employees with shift_id:
-- SELECT 
--     employee_id,
--     organization_id,
--     shift_id,
--     shift_name,
--     shift_source
-- FROM public.get_employees_with_shift_for_today()
-- WHERE shift_id IS NOT NULL;

-- Get employees without shift_id (for debugging):
-- SELECT 
--     employee_id,
--     employee_name,
--     organization_id,
--     shift_id,
--     shift_source,
--     has_schedule,
--     has_schedule_day,
--     has_exception
-- FROM public.get_employees_with_shift_for_today()
-- WHERE shift_id IS NULL
-- ORDER BY employee_name;

-- Get employees with schedule but no shift_id:
-- SELECT 
--     employee_id,
--     employee_name,
--     organization_id,
--     shift_id,
--     shift_source,
--     attendance_schedule_id,
--     has_schedule,
--     has_schedule_day,
--     has_exception
-- FROM public.get_employees_with_shift_for_today()
-- WHERE has_schedule = true AND shift_id IS NULL
-- ORDER BY employee_name;

-- ====================================================================
-- Notes:
-- ====================================================================
-- 1. This function returns all active employees with their shift information for TODAY
-- 2. Shift priority: Exception > ScheduleDay > Employee Default Shift > NULL
-- 3. The function shows the source of shift_id (Exception, ScheduleDay, or Employee Default)
-- 4. Includes flags to help debug why shift_id might be NULL
-- 5. Useful for troubleshooting attendance record creation
-- ====================================================================

