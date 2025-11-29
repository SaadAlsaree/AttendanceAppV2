using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "permission",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    description = table.Column<string>(type: "varchar(255)", nullable: false),
                    resource = table.Column<string>(type: "varchar(50)", nullable: false),
                    action = table.Column<string>(type: "varchar(50)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    blob_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceBreaks",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    break_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_breaks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    major = table.Column<int>(type: "integer", nullable: true),
                    minor = table.Column<int>(type: "integer", nullable: true),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    card_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    card_type = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    card_reader_no = table.Column<int>(type: "integer", nullable: true),
                    door_no = table.Column<int>(type: "integer", nullable: true),
                    employee_no_string = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    serial_no = table.Column<int>(type: "integer", nullable: true),
                    user_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    current_verify_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    attendance_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mask = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    picture_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    work_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_out_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    working_minutes = table.Column<int>(type: "integer", nullable: true),
                    break_minutes = table.Column<int>(type: "integer", nullable: true),
                    overtime_minutes = table.Column<int>(type: "integer", nullable: true),
                    late_minutes = table.Column<int>(type: "integer", nullable: true),
                    early_leave_minutes = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    check_in_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_out_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attendance_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSchedules",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    schedule_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    excluded_dates = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    isup_key = table.Column<string>(type: "text", nullable: true),
                    port = table.Column<string>(type: "text", nullable: true),
                    protocol = table.Column<string>(type: "text", nullable: true),
                    device_model = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    mac_address = table.Column<string>(type: "text", nullable: true),
                    firmware_version = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    features = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_connected = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rfid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    second_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    third_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fourth_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    family_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_manager = table.Column<bool>(type: "boolean", nullable: true),
                    face_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    national_id_front_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    national_id_back_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    profile_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Leaves",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaves", x => x.id);
                    table.ForeignKey(
                        name: "fk_leaves_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalUnits",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    parent_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit_level = table.Column<int>(type: "integer", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizational_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizational_units_employees_manager_id",
                        column: x => x.manager_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_organizational_units_organizational_units_parent_unit_id",
                        column: x => x.parent_unit_id,
                        principalSchema: "public",
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holidays", x => x.id);
                    table.ForeignKey(
                        name: "fk_holidays_organizational_units_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    shift_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    grace_period_minutes = table.Column<int>(type: "integer", nullable: true),
                    max_late_minutes = table.Column<int>(type: "integer", nullable: true),
                    allow_early_check_in = table.Column<bool>(type: "boolean", nullable: false),
                    allow_late_check_out = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_shifts_organizational_units_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_organizational_units_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalSchema: "public",
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkLocations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    radius_meters = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    wifi_ssid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    beacon_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_locations_organizational_units_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleDays",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_days_attendance_schedules_attendance_schedule_id",
                        column: x => x.attendance_schedule_id,
                        principalSchema: "public",
                        principalTable: "AttendanceSchedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_schedule_days_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "public",
                        principalTable: "Shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleIssues",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    exception_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_issues_attendance_schedules_attendance_schedule_id",
                        column: x => x.attendance_schedule_id,
                        principalSchema: "public",
                        principalTable: "AttendanceSchedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_schedule_issues_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "public",
                        principalTable: "Shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    additional_data = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_successful = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_latitude = table.Column<double>(type: "double precision", nullable: true),
                    location_longitude = table.Column<double>(type: "double precision", nullable: true),
                    biometric_id = table.Column<Guid>(type: "uuid", nullable: true),
                    biometric_confidence = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_audit_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_security_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "todo_items",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    labels = table.Column<List<string>>(type: "text[]", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_todo_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_todo_items_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permission",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    done_procdure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "public",
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_permission_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_employee_id",
                schema: "public",
                table: "Attachments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_breaks_attendance_id",
                schema: "public",
                table: "AttendanceBreaks",
                column: "attendance_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_breaks_break_type",
                schema: "public",
                table: "AttendanceBreaks",
                column: "break_type");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_breaks_end_time",
                schema: "public",
                table: "AttendanceBreaks",
                column: "end_time");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_breaks_start_time",
                schema: "public",
                table: "AttendanceBreaks",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_attendance_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "attendance_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_attendance_status",
                schema: "public",
                table: "AttendanceLogs",
                column: "attendance_status");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_card_no",
                schema: "public",
                table: "AttendanceLogs",
                column: "card_no");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_card_reader_no",
                schema: "public",
                table: "AttendanceLogs",
                column: "card_reader_no");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_door_no",
                schema: "public",
                table: "AttendanceLogs",
                column: "door_no");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_employee_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_organization_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_serial_no",
                schema: "public",
                table: "AttendanceLogs",
                column: "serial_no");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_time",
                schema: "public",
                table: "AttendanceLogs",
                column: "time");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_work_location_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "work_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_attendance_schedule_id",
                schema: "public",
                table: "Attendances",
                column: "attendance_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_date",
                schema: "public",
                table: "Attendances",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_employee_id_date",
                schema: "public",
                table: "Attendances",
                columns: new[] { "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_attendances_shift_id",
                schema: "public",
                table: "Attendances",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_status",
                schema: "public",
                table: "Attendances",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_schedules_employee_id_start_date",
                schema: "public",
                table: "AttendanceSchedules",
                columns: new[] { "employee_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_schedules_end_date",
                schema: "public",
                table: "AttendanceSchedules",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_schedules_is_active",
                schema: "public",
                table: "AttendanceSchedules",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_schedules_schedule_type",
                schema: "public",
                table: "AttendanceSchedules",
                column: "schedule_type");

            migrationBuilder.CreateIndex(
                name: "ix_devices_device_id",
                schema: "public",
                table: "Devices",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_ip_address",
                schema: "public",
                table: "Devices",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "ix_devices_organization_id",
                schema: "public",
                table: "Devices",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_code",
                schema: "public",
                table: "Employees",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                schema: "public",
                table: "Employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_employee_id",
                schema: "public",
                table: "Employees",
                column: "employee_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_full_name",
                schema: "public",
                table: "Employees",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_employees_manager_id",
                schema: "public",
                table: "Employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_organizational_unit_id",
                schema: "public",
                table: "Employees",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_rfid",
                schema: "public",
                table: "Employees",
                column: "rfid");

            migrationBuilder.CreateIndex(
                name: "ix_employees_shift_id",
                schema: "public",
                table: "Employees",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_user_id",
                schema: "public",
                table: "Employees",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_holidays_date",
                schema: "public",
                table: "Holidays",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_holidays_is_recurring",
                schema: "public",
                table: "Holidays",
                column: "is_recurring");

            migrationBuilder.CreateIndex(
                name: "ix_holidays_organization_id_date_is_recurring",
                schema: "public",
                table: "Holidays",
                columns: new[] { "organization_id", "date", "is_recurring" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leaves_employee_id",
                schema: "public",
                table: "Leaves",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_leaves_end_date",
                schema: "public",
                table: "Leaves",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "ix_leaves_leave_type",
                schema: "public",
                table: "Leaves",
                column: "leave_type");

            migrationBuilder.CreateIndex(
                name: "ix_leaves_start_date",
                schema: "public",
                table: "Leaves",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "ix_leaves_status",
                schema: "public",
                table: "Leaves",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_manager_id",
                schema: "public",
                table: "OrganizationalUnits",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_parent_unit_id",
                schema: "public",
                table: "OrganizationalUnits",
                column: "parent_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_unit_code",
                schema: "public",
                table: "OrganizationalUnits",
                column: "unit_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_unit_name",
                schema: "public",
                table: "OrganizationalUnits",
                column: "unit_name");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_days_attendance_schedule_id_day_of_week",
                schema: "public",
                table: "ScheduleDays",
                columns: new[] { "attendance_schedule_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schedule_days_is_active",
                schema: "public",
                table: "ScheduleDays",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_days_shift_id",
                schema: "public",
                table: "ScheduleDays",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_issues_attendance_schedule_id_date",
                schema: "public",
                table: "ScheduleIssues",
                columns: new[] { "attendance_schedule_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schedule_issues_date",
                schema: "public",
                table: "ScheduleIssues",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_issues_exception_type",
                schema: "public",
                table: "ScheduleIssues",
                column: "exception_type");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_issues_shift_id",
                schema: "public",
                table: "ScheduleIssues",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_device_id",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_employee_id",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_event_type",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_is_successful",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "is_successful");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_severity",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_timestamp",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_logs_user_id",
                schema: "public",
                table: "SecurityAuditLogs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shifts_is_active",
                schema: "public",
                table: "Shifts",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_shifts_organization_id_name",
                schema: "public",
                table: "Shifts",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shifts_shift_type",
                schema: "public",
                table: "Shifts",
                column: "shift_type");

            migrationBuilder.CreateIndex(
                name: "ix_todo_items_user_id",
                schema: "public",
                table: "todo_items",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_permission_id",
                schema: "public",
                table: "user_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_user_id",
                schema: "public",
                table: "user_permission",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_organizational_unit_id",
                schema: "public",
                table: "Users",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_user_login",
                schema: "public",
                table: "Users",
                column: "user_login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_locations_is_active",
                schema: "public",
                table: "WorkLocations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_work_locations_latitude_longitude",
                schema: "public",
                table: "WorkLocations",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_work_locations_organization_id_name",
                schema: "public",
                table: "WorkLocations",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_employees_employee_id",
                schema: "public",
                table: "Attachments",
                column: "employee_id",
                principalSchema: "public",
                principalTable: "Employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_breaks_attendances_attendance_id",
                schema: "public",
                table: "AttendanceBreaks",
                column: "attendance_id",
                principalSchema: "public",
                principalTable: "Attendances",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_logs_attendances_attendance_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "attendance_id",
                principalSchema: "public",
                principalTable: "Attendances",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_logs_employees_employee_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "employee_id",
                principalSchema: "public",
                principalTable: "Employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_logs_organizational_units_organization_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "organization_id",
                principalSchema: "public",
                principalTable: "OrganizationalUnits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_logs_work_locations_work_location_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "work_location_id",
                principalSchema: "public",
                principalTable: "WorkLocations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attendances_attendance_schedules_attendance_schedule_id",
                schema: "public",
                table: "Attendances",
                column: "attendance_schedule_id",
                principalSchema: "public",
                principalTable: "AttendanceSchedules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_attendances_employees_employee_id",
                schema: "public",
                table: "Attendances",
                column: "employee_id",
                principalSchema: "public",
                principalTable: "Employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_attendances_shifts_shift_id",
                schema: "public",
                table: "Attendances",
                column: "shift_id",
                principalSchema: "public",
                principalTable: "Shifts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_attendance_schedules_employees_employee_id",
                schema: "public",
                table: "AttendanceSchedules",
                column: "employee_id",
                principalSchema: "public",
                principalTable: "Employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_devices_organizational_units_organization_id",
                schema: "public",
                table: "Devices",
                column: "organization_id",
                principalSchema: "public",
                principalTable: "OrganizationalUnits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_organizational_units_organizational_unit_id",
                schema: "public",
                table: "Employees",
                column: "organizational_unit_id",
                principalSchema: "public",
                principalTable: "OrganizationalUnits",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_employees_shifts_shift_id",
                schema: "public",
                table: "Employees",
                column: "shift_id",
                principalSchema: "public",
                principalTable: "Shifts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_employees_users_user_id",
                schema: "public",
                table: "Employees",
                column: "user_id",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organizational_units_employees_manager_id",
                schema: "public",
                table: "OrganizationalUnits");

            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AttendanceBreaks",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AttendanceLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Leaves",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ScheduleDays",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ScheduleIssues",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "todo_items",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_permission",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "public");

            migrationBuilder.DropTable(
                name: "WorkLocations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "permission",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AttendanceSchedules",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OrganizationalUnits",
                schema: "public");
        }
    }
}
