using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_attendance_logs_attendances_attendance_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropForeignKey(
                name: "fk_attendance_logs_employees_employee_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropForeignKey(
                name: "fk_attendance_logs_organizational_units_organization_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropForeignKey(
                name: "fk_attendance_logs_work_locations_work_location_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_employees_employee_id",
                schema: "public",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_attendance_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_attendance_status",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_card_reader_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_door_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_employee_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_organization_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_serial_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_work_location_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "employee_id",
                schema: "public",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "attendance_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "attendance_status",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "card_reader_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "card_type",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "current_verify_mode",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "door_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "employee_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "employee_no_string",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "label",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "major",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "mask",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "minor",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "organization_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "picture_url",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "serial_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "user_type",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "work_location_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.RenameColumn(
                name: "time",
                schema: "public",
                table: "AttendanceLogs",
                newName: "date_time_attend");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "public",
                table: "AttendanceLogs",
                newName: "device_name");

            migrationBuilder.RenameIndex(
                name: "ix_attendance_logs_time",
                schema: "public",
                table: "AttendanceLogs",
                newName: "ix_attendance_logs_date_time_attend");

            migrationBuilder.AddColumn<string>(
                name: "emp_id",
                schema: "public",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "card_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_work",
                schema: "public",
                table: "AttendanceLogs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "device_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "direct",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "emp_id",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "time_attend",
                schema: "public",
                table: "AttendanceLogs",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "attendance_exceptions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exception_type = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: true),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_attendance_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_exceptions_attendances_attendance_id",
                        column: x => x.attendance_id,
                        principalSchema: "public",
                        principalTable: "Attendances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attendance_exceptions_employees_approver_id",
                        column: x => x.approver_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_attendance_exceptions_employees_modifier_id",
                        column: x => x.modifier_id,
                        principalSchema: "public",
                        principalTable: "Employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_emp_id",
                schema: "public",
                table: "Employees",
                column: "emp_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_date_work",
                schema: "public",
                table: "AttendanceLogs",
                column: "date_work");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_device_name",
                schema: "public",
                table: "AttendanceLogs",
                column: "device_name");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_device_no",
                schema: "public",
                table: "AttendanceLogs",
                column: "device_no");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_direct",
                schema: "public",
                table: "AttendanceLogs",
                column: "direct");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_emp_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "emp_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_logs_time_attend",
                schema: "public",
                table: "AttendanceLogs",
                column: "time_attend");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_approver_id",
                schema: "public",
                table: "attendance_exceptions",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_attendance_id",
                schema: "public",
                table: "attendance_exceptions",
                column: "attendance_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_modifier_id",
                schema: "public",
                table: "attendance_exceptions",
                column: "modifier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_exceptions",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_employees_emp_id",
                schema: "public",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_date_work",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_device_name",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_device_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_direct",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_emp_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "ix_attendance_logs_time_attend",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "emp_id",
                schema: "public",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "date_work",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "device_no",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "direct",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "emp_id",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "time_attend",
                schema: "public",
                table: "AttendanceLogs");

            migrationBuilder.RenameColumn(
                name: "device_name",
                schema: "public",
                table: "AttendanceLogs",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "date_time_attend",
                schema: "public",
                table: "AttendanceLogs",
                newName: "time");

            migrationBuilder.RenameIndex(
                name: "ix_attendance_logs_date_time_attend",
                schema: "public",
                table: "AttendanceLogs",
                newName: "ix_attendance_logs_time");

            migrationBuilder.AddColumn<string>(
                name: "employee_id",
                schema: "public",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "card_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "attendance_id",
                schema: "public",
                table: "AttendanceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attendance_status",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "card_reader_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "card_type",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_verify_mode",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "door_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id",
                schema: "public",
                table: "AttendanceLogs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<string>(
                name: "employee_no_string",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "label",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "major",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mask",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "minor",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                schema: "public",
                table: "AttendanceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "picture_url",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "serial_no",
                schema: "public",
                table: "AttendanceLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_type",
                schema: "public",
                table: "AttendanceLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "work_location_id",
                schema: "public",
                table: "AttendanceLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_employee_id",
                schema: "public",
                table: "Employees",
                column: "employee_id",
                unique: true);

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
                name: "ix_attendance_logs_work_location_id",
                schema: "public",
                table: "AttendanceLogs",
                column: "work_location_id");

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
        }
    }
}
