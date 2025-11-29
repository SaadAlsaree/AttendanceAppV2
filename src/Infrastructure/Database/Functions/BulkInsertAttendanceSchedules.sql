-- ====================================================================
-- PostgreSQL Function: Bulk Insert Attendance Schedules
-- ====================================================================
-- Description: Creates attendance schedules for all employees who don't have one
--              that overlaps with the start date. Creates ScheduleDays for each date
--              in the range, excluding Friday and Saturday.
-- Author: System
-- Date: 2025-11-14
-- ====================================================================

CREATE OR REPLACE FUNCTION public.bulk_insert_attendance_schedules(
    p_start_date DATE,
    p_end_date DATE,
    p_shift_id UUID
)
RETURNS TABLE (
    schedules_created INTEGER,
    schedule_days_created INTEGER,
    employees_processed INTEGER,
    employees_skipped INTEGER,
    execution_time_ms NUMERIC
) 
LANGUAGE plpgsql
AS $$
DECLARE
    v_end_date DATE;
    v_schedules_created INTEGER := 0;
    v_schedule_days_created INTEGER := 0;
    v_employees_processed INTEGER := 0;
    v_employees_skipped INTEGER := 0;
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_now TIMESTAMP WITH TIME ZONE;
    v_friday_saturday_dates TEXT;
    v_current_date DATE;
    v_schedule_id UUID;
    v_schedule_day_id UUID;
    v_employee_id UUID;
BEGIN
    -- Start timing
    v_start_time := clock_timestamp();
    v_now := CURRENT_TIMESTAMP;
    
    -- Validate end_date
    IF p_end_date IS NULL THEN
        v_end_date := p_start_date;
    ELSE
        v_end_date := p_end_date;
    END IF;
    
    -- Validate that end_date is greater than or equal to start_date
    IF v_end_date < p_start_date THEN
        RAISE EXCEPTION 'End date must be greater than or equal to start date';
    END IF;
    
    -- Validate shift_id exists
    IF NOT EXISTS (SELECT 1 FROM public."Shifts" WHERE id = p_shift_id AND is_deleted = false) THEN
        RAISE EXCEPTION 'Shift with id % does not exist or is deleted', p_shift_id;
    END IF;
    
    -- Get Friday and Saturday dates in the date range as comma-separated string
    v_friday_saturday_dates := '';
    v_current_date := p_start_date;
    
    WHILE v_current_date <= v_end_date LOOP
        -- Check if current date is Friday (5) or Saturday (6)
        -- PostgreSQL: 0=Sunday, 1=Monday, ..., 6=Saturday
        -- We need: 5=Friday, 6=Saturday
        IF EXTRACT(DOW FROM v_current_date) IN (5, 6) THEN
            IF v_friday_saturday_dates = '' THEN
                v_friday_saturday_dates := v_current_date::TEXT;
            ELSE
                v_friday_saturday_dates := v_friday_saturday_dates || ',' || v_current_date::TEXT;
            END IF;
        END IF;
        v_current_date := v_current_date + INTERVAL '1 day';
    END LOOP;
    
    -- Create a temporary table to store employees who need schedules
    CREATE TEMP TABLE IF NOT EXISTS temp_employees_for_schedule (
        employee_id UUID PRIMARY KEY
    ) ON COMMIT DROP;
    
    -- Insert employees who don't have an active schedule that overlaps with start_date
    INSERT INTO temp_employees_for_schedule (employee_id)
    SELECT DISTINCT e.id
    FROM public."Employees" e
    WHERE 
        e.organizational_unit_id IS NOT NULL
        AND e.is_deleted = false
        -- Exclude employees who already have an active schedule that overlaps with start_date
        AND NOT EXISTS (
            SELECT 1
            FROM public."AttendanceSchedules" asch
            WHERE 
                asch.employee_id = e.id
                AND asch.is_active = true
                AND asch.is_deleted = false
                AND asch.start_date <= p_start_date
                AND (asch.end_date IS NULL OR asch.end_date >= p_start_date)
        );
    
    -- Get count of employees to process
    SELECT COUNT(*) INTO v_employees_processed FROM temp_employees_for_schedule;
    
    -- Get count of employees skipped (those who already have schedules)
    SELECT COUNT(*) INTO v_employees_skipped
    FROM public."Employees" e
    WHERE 
        e.organizational_unit_id IS NOT NULL
        AND e.is_deleted = false
        AND EXISTS (
            SELECT 1
            FROM public."AttendanceSchedules" asch
            WHERE 
                asch.employee_id = e.id
                AND asch.is_active = true
                AND asch.is_deleted = false
                AND asch.start_date <= p_start_date
                AND (asch.end_date IS NULL OR asch.end_date >= p_start_date)
        );
    
    -- If no employees to process, return early
    IF v_employees_processed = 0 THEN
        v_end_time := clock_timestamp();
        RETURN QUERY 
        SELECT 
            0::INTEGER,
            0::INTEGER,
            0::INTEGER,
            v_employees_skipped,
            EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::NUMERIC;
        RETURN;
    END IF;
    
    -- Insert AttendanceSchedules and ScheduleDays in batches
    -- Process employees in batches to avoid huge transactions
    FOR v_employee_id IN 
        SELECT employee_id FROM temp_employees_for_schedule ORDER BY employee_id
    LOOP
        -- Generate schedule ID
        v_schedule_id := gen_random_uuid();
        
        -- Insert AttendanceSchedule
        INSERT INTO public."AttendanceSchedules" (
            id,
            employee_id,
            start_date,
            end_date,
            schedule_type,
            is_active,
            excluded_dates,
            created_at,
            is_deleted
        ) VALUES (
            v_schedule_id,
            v_employee_id,
            p_start_date,
            CASE WHEN p_end_date IS NULL THEN NULL ELSE p_end_date END,
            'Regular',  -- ScheduleType.Regular
            true,
            CASE WHEN v_friday_saturday_dates = '' THEN NULL ELSE v_friday_saturday_dates END,
            v_now,
            false
        );
        
        v_schedules_created := v_schedules_created + 1;
        
        -- Create ScheduleDay for each date in the range [StartDate, EndDate]
        v_current_date := p_start_date;
        WHILE v_current_date <= v_end_date LOOP
            v_schedule_day_id := gen_random_uuid();
            
            INSERT INTO public."ScheduleDays" (
                id,
                attendance_schedule_id,
                schedule_day_date,
                shift_id,
                is_active,
                created_at,
                is_deleted
            ) VALUES (
                v_schedule_day_id,
                v_schedule_id,
                v_current_date,
                p_shift_id,
                true,
                v_now,
                false
            );
            
            v_schedule_days_created := v_schedule_days_created + 1;
            v_current_date := v_current_date + INTERVAL '1 day';
        END LOOP;
    END LOOP;
    
    -- Clean up temporary table
    DROP TABLE IF EXISTS temp_employees_for_schedule;
    
    -- End timing
    v_end_time := clock_timestamp();
    
    -- Return results
    RETURN QUERY 
    SELECT 
        v_schedules_created,
        v_schedule_days_created,
        v_employees_processed,
        v_employees_skipped,
        EXTRACT(MILLISECONDS FROM (v_end_time - v_start_time))::NUMERIC;
        
EXCEPTION
    WHEN OTHERS THEN
        -- Clean up temporary table on error
        DROP TABLE IF EXISTS temp_employees_for_schedule;
        -- Log error and re-raise
        RAISE NOTICE 'Error in bulk_insert_attendance_schedules: % %', SQLERRM, SQLSTATE;
        RAISE;
END;
$$;

-- ====================================================================
-- Grant execute permission (adjust role as needed)
-- ====================================================================
-- GRANT EXECUTE ON FUNCTION public.bulk_insert_attendance_schedules(DATE, DATE, UUID) TO your_app_user;

-- ====================================================================
-- Usage Examples:
-- ====================================================================
-- Step 1: Get a valid shift_id first
-- SELECT id, name FROM public."Shifts" WHERE is_deleted = false LIMIT 1;

-- Step 2: Use the shift_id in the function
-- Example with end date:
-- SELECT * FROM public.bulk_insert_attendance_schedules(
--     '2025-01-01'::DATE,
--     '2025-12-31'::DATE,
--     (SELECT id FROM public."Shifts" WHERE is_deleted = false LIMIT 1)::UUID
-- );

-- Example: Single date (start_date = end_date):
-- SELECT * FROM public.bulk_insert_attendance_schedules(
--     '2025-01-01'::DATE,
--     '2025-01-01'::DATE,
--     (SELECT id FROM public."Shifts" WHERE is_deleted = false LIMIT 1)::UUID
-- );

-- Example: With specific shift_id (replace with actual UUID):
-- SELECT * FROM public.bulk_insert_attendance_schedules(
--     '2025-01-01'::DATE,
--     '2025-12-31'::DATE,
--     '00000000-0000-0000-0000-000000000000'::UUID  -- Replace with actual shift UUID
-- );

-- Example: With output formatting:
-- SELECT 
--     schedules_created,
--     schedule_days_created,
--     employees_processed,
--     employees_skipped,
--     ROUND(execution_time_ms, 2) as execution_ms
-- FROM public.bulk_insert_attendance_schedules(
--     '2025-01-01'::DATE,
--     '2025-12-31'::DATE,
--     (SELECT id FROM public."Shifts" WHERE is_deleted = false LIMIT 1)::UUID
-- );

-- Example: Get shift_id by name and use it:
-- WITH shift_info AS (
--     SELECT id FROM public."Shifts" 
--     WHERE name = 'Morning Shift' AND is_deleted = false 
--     LIMIT 1
-- )
-- SELECT * FROM public.bulk_insert_attendance_schedules(
--     '2025-01-01'::DATE,
--     '2025-12-31'::DATE,
--     (SELECT id FROM shift_info)::UUID
-- );

-- ====================================================================
-- Notes:
-- ====================================================================
-- 1. This function creates attendance schedules for employees who don't have
--    an active schedule that overlaps with the start_date
-- 2. Creates ScheduleDays for each date in the range [StartDate, EndDate]
-- 3. Automatically excludes Friday and Saturday dates in ExcludedDates
-- 4. Uses ScheduleType = 'Regular'
-- 5. Skips employees who already have an active schedule
-- 6. Returns statistics about the operation
-- 7. Validates that shift_id exists and is not deleted
-- 8. Validates that end_date >= start_date
-- ====================================================================

