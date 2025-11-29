using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "employee_id1",
                schema: "public",
                table: "Leaves",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_leaves_employee_id1",
                schema: "public",
                table: "Leaves",
                column: "employee_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_leaves_employees_employee_id1",
                schema: "public",
                table: "Leaves",
                column: "employee_id1",
                principalSchema: "public",
                principalTable: "Employees",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_leaves_employees_employee_id1",
                schema: "public",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "ix_leaves_employee_id1",
                schema: "public",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "employee_id1",
                schema: "public",
                table: "Leaves");
        }
    }
}
