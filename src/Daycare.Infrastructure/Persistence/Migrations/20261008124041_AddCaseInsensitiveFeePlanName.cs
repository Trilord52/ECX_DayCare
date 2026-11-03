using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Daycare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveFeePlanName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fee_plans_name",
                table: "fee_plans");

            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_fee_plans_lower_name ON fee_plans (lower(name));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_fee_plans_lower_name;");

            migrationBuilder.CreateIndex(
                name: "ix_fee_plans_name",
                table: "fee_plans",
                column: "name",
                unique: true);
        }
    }
}
