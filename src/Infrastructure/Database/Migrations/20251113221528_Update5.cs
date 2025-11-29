using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attendances_employee_id_date",
                schema: "public",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_employee_id_date",
                schema: "public",
                table: "Attendances",
                columns: new[] { "employee_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attendances_employee_id_date",
                schema: "public",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "ix_attendances_employee_id_date",
                schema: "public",
                table: "Attendances",
                columns: new[] { "employee_id", "date" });
        }
    }
}
