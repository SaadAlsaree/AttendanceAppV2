using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shifts_organizational_units_organization_id",
                schema: "public",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "ix_shifts_organization_id_name",
                schema: "public",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "organization_id",
                schema: "public",
                table: "Shifts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                schema: "public",
                table: "Shifts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "ix_shifts_organization_id_name",
                schema: "public",
                table: "Shifts",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_shifts_organizational_units_organization_id",
                schema: "public",
                table: "Shifts",
                column: "organization_id",
                principalSchema: "public",
                principalTable: "OrganizationalUnits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
