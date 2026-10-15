using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionHoldMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConditionSurveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DamageCodes = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhotoRefs = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionSurveys_ContainerVisits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "ContainerVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovementEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovementEvents_ContainerVisits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "ContainerVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StayHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HeldBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReleaseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayHolds_ContainerVisits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "ContainerVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConditionSurveys_TenantId",
                table: "ConditionSurveys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionSurveys_TenantId_VisitId_InspectedAt",
                table: "ConditionSurveys",
                columns: new[] { "TenantId", "VisitId", "InspectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConditionSurveys_VisitId",
                table: "ConditionSurveys",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementEvents_TenantId",
                table: "MovementEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementEvents_TenantId_VisitId_OccurredAt",
                table: "MovementEvents",
                columns: new[] { "TenantId", "VisitId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementEvents_VisitId",
                table: "MovementEvents",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_StayHolds_TenantId",
                table: "StayHolds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StayHolds_TenantId_VisitId_ReleasedAt",
                table: "StayHolds",
                columns: new[] { "TenantId", "VisitId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StayHolds_VisitId",
                table: "StayHolds",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConditionSurveys");

            migrationBuilder.DropTable(
                name: "MovementEvents");

            migrationBuilder.DropTable(
                name: "StayHolds");
        }
    }
}
