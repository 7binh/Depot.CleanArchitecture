using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerStay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerStays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Condition = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DamageNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InVehicleType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    InVehicleNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: true),
                    GateOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OutVehicleType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OutVehicleNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReleaseOrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerStays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerStays_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerStays_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerStays_ReleaseOrderItems_ReleaseOrderItemId",
                        column: x => x.ReleaseOrderItemId,
                        principalTable: "ReleaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerStays_YardRows_RowId",
                        column: x => x.RowId,
                        principalTable: "YardRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerStays_BlockId",
                table: "ContainerStays",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerStays_ContainerId",
                table: "ContainerStays",
                column: "ContainerId",
                unique: true,
                filter: "\"GateOutAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerStays_ReleaseOrderItemId",
                table: "ContainerStays",
                column: "ReleaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerStays_RowId_Tier",
                table: "ContainerStays",
                columns: new[] { "RowId", "Tier" },
                unique: true,
                filter: "\"GateOutAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerStays");
        }
    }
}
