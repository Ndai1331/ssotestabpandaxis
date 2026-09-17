using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    [DbContext(typeof(WorkManagementDbContext))]
    [Migration("20260917040000_AddSurveyHandling")]
    public partial class AddSurveyHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandlingNote",
                schema: "hcs_work",
                table: "SurveySessions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandlingStatus",
                schema: "hcs_work",
                table: "SurveySessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandlingNote",
                schema: "hcs_work",
                table: "SurveySessions");

            migrationBuilder.DropColumn(
                name: "HandlingStatus",
                schema: "hcs_work",
                table: "SurveySessions");
        }
    }
}
