using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schedule_days_attendance_schedule_id_day_of_week",
                schema: "public",
                table: "ScheduleDays");

            migrationBuilder.DropColumn(
                name: "day_of_week",
                schema: "public",
                table: "ScheduleDays");

            migrationBuilder.AddColumn<DateOnly>(
                name: "schedule_day_date",
                schema: "public",
                table: "ScheduleDays",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "ix_schedule_days_attendance_schedule_id_schedule_day_date",
                schema: "public",
                table: "ScheduleDays",
                columns: new[] { "attendance_schedule_id", "schedule_day_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_schedule_days_attendance_schedule_id_schedule_day_date",
                schema: "public",
                table: "ScheduleDays");

            migrationBuilder.DropColumn(
                name: "schedule_day_date",
                schema: "public",
                table: "ScheduleDays");

            migrationBuilder.AddColumn<string>(
                name: "day_of_week",
                schema: "public",
                table: "ScheduleDays",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_days_attendance_schedule_id_day_of_week",
                schema: "public",
                table: "ScheduleDays",
                columns: new[] { "attendance_schedule_id", "day_of_week" },
                unique: true);
        }
    }
}
