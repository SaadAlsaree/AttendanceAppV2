using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "site_id",
                schema: "public",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "site_id",
                schema: "public",
                table: "OrganizationalUnits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sites",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    site_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_sites", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_site_id",
                schema: "public",
                table: "Users",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_site_id",
                schema: "public",
                table: "OrganizationalUnits",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_sites_site_code",
                schema: "public",
                table: "Sites",
                column: "site_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sites_site_name",
                schema: "public",
                table: "Sites",
                column: "site_name");

            migrationBuilder.AddForeignKey(
                name: "fk_organizational_units_sites_site_id",
                schema: "public",
                table: "OrganizationalUnits",
                column: "site_id",
                principalSchema: "public",
                principalTable: "Sites",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_sites_site_id",
                schema: "public",
                table: "Users",
                column: "site_id",
                principalSchema: "public",
                principalTable: "Sites",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organizational_units_sites_site_id",
                schema: "public",
                table: "OrganizationalUnits");

            migrationBuilder.DropForeignKey(
                name: "fk_users_sites_site_id",
                schema: "public",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Sites",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_users_site_id",
                schema: "public",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "ix_organizational_units_site_id",
                schema: "public",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "site_id",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "site_id",
                schema: "public",
                table: "OrganizationalUnits");
        }
    }
}
