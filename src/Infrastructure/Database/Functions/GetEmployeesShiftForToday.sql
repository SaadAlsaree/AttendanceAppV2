-- ====================================================================
-- PostgreSQL Function: Get Employees Shift For Today (Simple)
-- ====================================================================
-- Description: Returns simple list of employees with EmployeeId, OrganizationId, ShiftId for today
-- Author: System
-- Date: 2025-11-14
-- ====================================================================

CREATE OR REPLACE FUNCTION public.get_employees_shift_for_today()
RETURNS TABLE (
    employee_id UUID,
    organization_id UUID,
    shift_id UUID
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
            asch.employee_id
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
            si.shift_id
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
            sd.shift_id
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
        e.organizational_unit_id AS organization_id,
        -- Get shift_id: Priority 1 - Exception, Priority 2 - ScheduleDay, Priority 3 - Employee's default shift
        COALESCE(
            se.shift_id,      -- Priority 1: From ScheduleIssue (exception)
            sd.shift_id,      -- Priority 2: From ScheduleDay
            e.shift_id        -- Priority 3: Employee's default shift
        ) AS shift_id
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
        e.id;
END;
$$;

-- ====================================================================
-- Grant execute permission (adjust role as needed)
-- ====================================================================
-- GRANT EXECUTE ON FUNCTION public.get_employees_shift_for_today() TO your_app_user;

-- ====================================================================
-- Usage Examples:
-- ====================================================================
-- Basic usage:
-- SELECT * FROM public.get_employees_shift_for_today();

-- Get only employees with shift_id:
-- SELECT * FROM public.get_employees_shift_for_today()
-- WHERE shift_id IS NOT NULL;

-- Get employees without shift_id (for debugging):
-- SELECT * FROM public.get_employees_shift_for_today()
-- WHERE shift_id IS NULL;

-- Count employees by shift status:
-- SELECT 
--     CASE 
--         WHEN shift_id IS NULL THEN 'No Shift'
--         ELSE 'Has Shift'
--     END AS shift_status,
--     COUNT(*) AS employee_count
-- FROM public.get_employees_shift_for_today()
-- GROUP BY 
--     CASE 
--         WHEN shift_id IS NULL THEN 'No Shift'
--         ELSE 'Has Shift'
--     END;

-- ====================================================================
-- Notes:
-- ====================================================================
-- 1. This function returns EmployeeId, OrganizationId, ShiftId for TODAY
-- 2. Shift priority: Exception > ScheduleDay > Employee Default Shift > NULL
-- 3. Only returns active employees with organizational_unit_id
-- 4. Simple output format for easy integration
-- ====================================================================

