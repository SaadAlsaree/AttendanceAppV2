-- ====================================================================
-- PostgreSQL Function: Create Daily Attendance Records
-- ====================================================================
-- Description: Creates attendance records for all active employees for today
-- Author: System
-- Date: 2025-11-14
-- ====================================================================

CREATE OR REPLACE FUNCTION public.create_daily_attendance_records()
RETURNS TABLE (
    records_created INTEGER,
    records_skipped INTEGER,
    execution_time_ms NUMERIC
) 
LANGUAGE plpgsql
AS $$
DECLARE
    v_today DATE;
    v_today_start TIMESTAMP WITH TIME ZONE;
    v_today_end TIMESTAMP WITH TIME ZONE;
    v_records_created INTEGER := 0;
    v_records_skipped INTEGER := 0;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
BEGIN
    -- Start timing
    v_start_time := clock_timestamp();
    
    -- Get today's date
    v_today := CURRENT_DATE;
    v_today_start := v_today::TIMESTAMP WITH TIME ZONE;
    v_today_end := (v_today + INTERVAL '1 day')::TIMESTAMP WITH TIME ZONE - INTERVAL '1 microsecond';

    -- Insert attendance records using direct subqueries (no CTEs for simplicity)
    INSERT INTO public."Attendances" (
        id,
        employee_id,
        organization_id,
        date,
        shift_id,
        attendance_schedule_id,
        status,
        created_at,
        is_deleted
    )
    SELECT 
        gen_random_uuid() AS id,
        e.id AS employee_id,
        e.organizational_unit_id AS organization_id,
        v_today_start AS date,
        -- Get shift_id with priority: ScheduleDay > Employee Default Shift
        COALESCE(
            -- Priority 1: Get shift from ScheduleDay for today
            (
                SELECT sd.shift_id
                FROM public."ScheduleDays" sd
                INNER JOIN public."AttendanceSchedules" asch ON asch.id = sd.attendance_schedule_id
                WHERE 
                    asch.employee_id = e.id
                    AND sd.schedule_day_date = v_today
                    AND sd.is_active = true
                    AND sd.is_deleted = false
                    AND asch.is_active = true
                    AND asch.is_deleted = false
                    AND asch.start_date <= v_today
                    AND (asch.end_date IS NULL OR asch.end_date >= v_today)
                    -- Check if today is NOT in excluded_dates
                    -- AND (
                    --     asch.excluded_dates IS NULL 
                    --     OR asch.excluded_dates = ''
                    --     OR NOT (v_today::text = ANY(string_to_array(asch.excluded_dates, ',')))
                    -- )
                ORDER BY sd.created_at DESC
                LIMIT 1
            ),
            -- Priority 2: Employee's default shift
            e.shift_id
        ) AS shift_id,
        -- Get attendance_schedule_id
        (
            SELECT asch.id
            FROM public."AttendanceSchedules" asch
            WHERE 
                asch.employee_id = e.id
                AND asch.is_active = true
                AND asch.is_deleted = false
                AND asch.start_date <= v_today
                AND (asch.end_date IS NULL OR asch.end_date >= v_today)
                -- Check if today is NOT in excluded_dates
                AND (
                    asch.excluded_dates IS NULL 
                    OR asch.excluded_dates = ''
                    OR NOT (v_today::text = ANY(string_to_array(asch.excluded_dates, ',')))
                )
            ORDER BY asch.created_at DESC
            LIMIT 1
        ) AS attendance_schedule_id,
        'Pending' AS status,
        CURRENT_TIMESTAMP AS created_at,
        false AS is_deleted
    FROM 
        public."Employees" e
    WHERE 
        -- Employee must have an organizational unit
        e.organizational_unit_id IS NOT NULL
        -- Employee must not be deleted
        AND e.is_deleted = false
        -- Attendance record must not already exist for today
        AND NOT EXISTS (
            SELECT 1 
            FROM public."Attendances" a 
            WHERE 
                a.employee_id = e.id 
                AND a.date >= v_today_start 
                AND a.date < v_today_end
                AND a.is_deleted = false
        );

    -- Get the number of records inserted
    GET DIAGNOSTICS v_records_created = ROW_COUNT;

    -- Count how many employees were skipped (already have attendance)
    SELECT COUNT(*)
    INTO v_records_skipped
    FROM public."Employees" e
    WHERE 
        e.organizational_unit_id IS NOT NULL
        AND e.is_deleted = false
        AND EXISTS (
            SELECT 1 
            FROM public."Attendances" a 
            WHERE 
                a.employee_id = e.id 
                AND a.date >= v_today_start 
                AND a.date < v_today_end
                AND a.is_deleted = false
        );

    -- End timing
    v_end_time := clock_timestamp();

    -- Return results
    RETURN QUERY 
    SELECT 
        v_records_created,
        v_records_skipped,
        EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::NUMERIC;

EXCEPTION
    WHEN OTHERS THEN
        -- Log error and re-raise
        RAISE NOTICE 'Error creating daily attendance records: % %', SQLERRM, SQLSTATE;
        RAISE;
END;
$$;

-- ====================================================================
-- Grant execute permission (adjust role as needed)
-- ====================================================================
-- GRANT EXECUTE ON FUNCTION public.create_daily_attendance_records() TO your_app_user;

-- ====================================================================
-- Usage Examples:
-- ====================================================================
-- Basic usage:
-- SELECT * FROM public.create_daily_attendance_records();

-- With output:
-- SELECT 
--     records_created,
--     records_skipped,
--     ROUND(execution_time_ms, 2) as execution_ms
-- FROM public.create_daily_attendance_records();

-- ====================================================================
-- Notes:
-- ====================================================================
-- 1. This function creates attendance records for TODAY only
-- 2. It automatically skips employees who already have attendance for today
-- 3. Shift priority: ScheduleDay > Employee Default Shift > NULL
-- 4. The function is idempotent - safe to run multiple times per day
-- 5. Returns statistics about the operation
-- 6. Uses direct subqueries instead of CTEs for better reliability
-- ====================================================================

-- ====================================================================
-- Diagnostic Queries: Test shift_id retrieval
-- ====================================================================
-- Query 1: Test ScheduleDays retrieval for specific employee
/*
SELECT 
    e.id AS employee_id,
    e.full_name,
    asch.id AS schedule_id,
    sd.schedule_day_date,
    sd.shift_id,
    sd.is_active,
    s.name AS shift_name
FROM public."Employees" e
INNER JOIN public."AttendanceSchedules" asch ON asch.employee_id = e.id 
    AND asch.is_active = true 
    AND asch.is_deleted = false
    AND asch.start_date <= CURRENT_DATE
    AND (asch.end_date IS NULL OR asch.end_date >= CURRENT_DATE)
INNER JOIN public."ScheduleDays" sd ON sd.attendance_schedule_id = asch.id
    AND sd.schedule_day_date = CURRENT_DATE
    AND sd.is_active = true
    AND sd.is_deleted = false
LEFT JOIN public."Shifts" s ON s.id = sd.shift_id
WHERE 
    e.id = '5a8a659c-d786-453f-8e53-a1bb8d18c0d7'
    AND e.organizational_unit_id IS NOT NULL
    AND e.is_deleted = false;
*/

-- Query 2: Test the exact subquery used in the function
/*
SELECT 
    e.id AS employee_id,
    e.full_name,
    (
        SELECT sd.shift_id
        FROM public."ScheduleDays" sd
        INNER JOIN public."AttendanceSchedules" asch ON asch.id = sd.attendance_schedule_id
        WHERE 
            asch.employee_id = e.id
            AND sd.schedule_day_date = CURRENT_DATE
            AND sd.is_active = true
            AND sd.is_deleted = false
            AND asch.is_active = true
            AND asch.is_deleted = false
            AND asch.start_date <= CURRENT_DATE
            AND (asch.end_date IS NULL OR asch.end_date >= CURRENT_DATE)
        ORDER BY sd.created_at DESC
        LIMIT 1
    ) AS shift_id_from_schedule_day
FROM public."Employees" e
WHERE 
    e.id = '5a8a659c-d786-453f-8e53-a1bb8d18c0d7'
    AND e.organizational_unit_id IS NOT NULL
    AND e.is_deleted = false;
*/

-- Query 3: Check all ScheduleDays for today (to verify data exists)
/*
SELECT 
    sd.id,
    sd.attendance_schedule_id,
    sd.schedule_day_date,
    sd.shift_id,
    sd.is_active,
    asch.employee_id,
    e.full_name
FROM public."ScheduleDays" sd
INNER JOIN public."AttendanceSchedules" asch ON asch.id = sd.attendance_schedule_id
INNER JOIN public."Employees" e ON e.id = asch.employee_id
WHERE 
    sd.schedule_day_date = CURRENT_DATE
    AND sd.is_active = true
    AND sd.is_deleted = false
    AND asch.is_active = true
    AND asch.is_deleted = false
ORDER BY e.full_name;
*/
-- ====================================================================
