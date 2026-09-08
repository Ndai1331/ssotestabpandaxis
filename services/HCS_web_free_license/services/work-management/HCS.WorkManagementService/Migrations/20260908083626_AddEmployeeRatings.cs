using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeRatings",
                schema: "hcs_work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRatings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRatings_DailyVote",
                schema: "hcs_work",
                table: "EmployeeRatings",
                columns: new[] { "VoterUserId", "TargetUserId", "EvaluationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRatings_TargetUserId_CreationTime",
                schema: "hcs_work",
                table: "EmployeeRatings",
                columns: new[] { "TargetUserId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRatings_VoterUserId",
                schema: "hcs_work",
                table: "EmployeeRatings",
                column: "VoterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeRatings",
                schema: "hcs_work");
        }
    }
}
