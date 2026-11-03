using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Daycare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                table: "staff",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_key",
                table: "staff",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                table: "staff",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_updated_at",
                table: "staff",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                table: "guardians",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_key",
                table: "guardians",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                table: "guardians",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_updated_at",
                table: "guardians",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                table: "children",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_key",
                table: "children",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                table: "children",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_updated_at",
                table: "children",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_staff_photo_all_or_none",
                table: "staff",
                sql: "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_guardians_photo_all_or_none",
                table: "guardians",
                sql: "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_children_photo_all_or_none",
                table: "children",
                sql: "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_staff_photo_all_or_none",
                table: "staff");

            migrationBuilder.DropCheckConstraint(
                name: "ck_guardians_photo_all_or_none",
                table: "guardians");

            migrationBuilder.DropCheckConstraint(
                name: "ck_children_photo_all_or_none",
                table: "children");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "photo_key",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "photo_updated_at",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                table: "guardians");

            migrationBuilder.DropColumn(
                name: "photo_key",
                table: "guardians");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                table: "guardians");

            migrationBuilder.DropColumn(
                name: "photo_updated_at",
                table: "guardians");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                table: "children");

            migrationBuilder.DropColumn(
                name: "photo_key",
                table: "children");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                table: "children");

            migrationBuilder.DropColumn(
                name: "photo_updated_at",
                table: "children");
        }
    }
}
