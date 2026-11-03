using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Daycare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveClassroomAndPaymentActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_classrooms_name",
                table: "classrooms");

            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_classrooms_lower_name ON classrooms (lower(name));");

            migrationBuilder.AddColumn<Guid>(
                name: "voided_by_user_id",
                table: "payments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "voided_by_user_id",
                table: "payments");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_classrooms_lower_name;");

            migrationBuilder.CreateIndex(
                name: "ix_classrooms_name",
                table: "classrooms",
                column: "name",
                unique: true);
        }
    }
}
