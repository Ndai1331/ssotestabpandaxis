using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    /// <inheritdoc />
    public partial class AddEventManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedEvents",
                schema: "hcs_work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Location = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    QrToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ManagedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventAttachments",
                schema: "hcs_work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttachments_ManagedEvents_EventId",
                        column: x => x.EventId,
                        principalSchema: "hcs_work",
                        principalTable: "ManagedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendees",
                schema: "hcs_work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Surname = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Cccd = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RegistrationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CheckInStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttendees_ManagedEvents_EventId",
                        column: x => x.EventId,
                        principalSchema: "hcs_work",
                        principalTable: "ManagedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_BlobName",
                schema: "hcs_work",
                table: "EventAttachments",
                column: "BlobName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_EventId",
                schema: "hcs_work",
                table: "EventAttachments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_UploadedByUserId",
                schema: "hcs_work",
                table: "EventAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId_Cccd",
                schema: "hcs_work",
                table: "EventAttendees",
                columns: new[] { "EventId", "Cccd" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId_Email",
                schema: "hcs_work",
                table: "EventAttendees",
                columns: new[] { "EventId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId_FullName",
                schema: "hcs_work",
                table: "EventAttendees",
                columns: new[] { "EventId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId_PhoneNumber",
                schema: "hcs_work",
                table: "EventAttendees",
                columns: new[] { "EventId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UserId",
                schema: "hcs_work",
                table: "EventAttendees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedEvents_Code",
                schema: "hcs_work",
                table: "ManagedEvents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedEvents_OwnerUserId",
                schema: "hcs_work",
                table: "ManagedEvents",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedEvents_QrToken",
                schema: "hcs_work",
                table: "ManagedEvents",
                column: "QrToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedEvents_Status_StartTime",
                schema: "hcs_work",
                table: "ManagedEvents",
                columns: new[] { "Status", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventAttachments",
                schema: "hcs_work");

            migrationBuilder.DropTable(
                name: "EventAttendees",
                schema: "hcs_work");

            migrationBuilder.DropTable(
                name: "ManagedEvents",
                schema: "hcs_work");
        }
    }
}
