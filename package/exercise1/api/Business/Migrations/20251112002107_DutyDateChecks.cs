using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StargateAPI.Migrations
{
    /// <inheritdoc />
    public partial class DutyDateChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_AstronautDuty_DutyDates",
                table: "AstronautDuty",
                sql: "DutyEndDate IS NULL OR DutyEndDate >= DutyStartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AstronautDuty_DutyDates",
                table: "AstronautDuty");
        }
    }
}
