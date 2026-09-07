using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveySessions_LocationId",
                schema: "hcs_work",
                table: "SurveySessions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTaskAssignments_UserId",
                schema: "hcs_work",
                table: "ProjectTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventParticipants_UserId",
                schema: "hcs_work",
                table: "CalendarEventParticipants");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySessions_LocationId_StartsAt",
                schema: "hcs_work",
                table: "SurveySessions",
                columns: new[] { "LocationId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAssignments_UserId_ProjectTaskId",
                schema: "hcs_work",
                table: "ProjectTaskAssignments",
                columns: new[] { "UserId", "ProjectTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventParticipants_UserId_CalendarEventId",
                schema: "hcs_work",
                table: "CalendarEventParticipants",
                columns: new[] { "UserId", "CalendarEventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveySessions_LocationId_StartsAt",
                schema: "hcs_work",
                table: "SurveySessions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTaskAssignments_UserId_ProjectTaskId",
                schema: "hcs_work",
                table: "ProjectTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventParticipants_UserId_CalendarEventId",
                schema: "hcs_work",
                table: "CalendarEventParticipants");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySessions_LocationId",
                schema: "hcs_work",
                table: "SurveySessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAssignments_UserId",
                schema: "hcs_work",
                table: "ProjectTaskAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventParticipants_UserId",
                schema: "hcs_work",
                table: "CalendarEventParticipants",
                column: "UserId");
        }
    }
}
