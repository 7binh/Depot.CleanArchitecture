using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    LineOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsoCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    TareWeight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxGrossWeight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    DamageNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LineOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CustomerTaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VesselName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VoyageNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YardBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxBay = table.Column<int>(type: "integer", nullable: true),
                    MaxRow = table.Column<int>(type: "integer", nullable: true),
                    MaxTier = table.Column<int>(type: "integer", nullable: true),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: true),
                    HasReeferPower = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RequiredGrade = table.Column<int>(type: "integer", nullable: false),
                    OrderedQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeliveredQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItems_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YardSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bay = table.Column<int>(type: "integer", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccupiedContainerSize = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YardSlots_YardBlocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "YardBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContainerVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentBlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    GateInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InVehicle_TractorNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InVehicle_TrailerNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InVehicle_DriverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InVehicle_DriverPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InSurveyGrade = table.Column<int>(type: "integer", nullable: false),
                    InDamageNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GateOutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OutVehicle_TractorNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OutVehicle_TrailerNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OutVehicle_DriverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OutVehicle_DriverPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerVisits_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerVisits_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContainerVisits_YardBlocks_CurrentBlockId",
                        column: x => x.CurrentBlockId,
                        principalTable: "YardBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContainerVisits_YardSlots_CurrentSlotId",
                        column: x => x.CurrentSlotId,
                        principalTable: "YardSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Containers_TenantId",
                table: "Containers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_TenantId_Number",
                table: "Containers",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_ContainerId",
                table: "ContainerVisits",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_CurrentBlockId",
                table: "ContainerVisits",
                column: "CurrentBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_CurrentSlotId",
                table: "ContainerVisits",
                column: "CurrentSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_DeliveryOrderId",
                table: "ContainerVisits",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_TenantId",
                table: "ContainerVisits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVisits_TenantId_Status_GateInDate_GateOutDate",
                table: "ContainerVisits",
                columns: new[] { "TenantId", "Status", "GateInDate", "GateOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItems_DeliveryOrderId",
                table: "DeliveryOrderItems",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_TenantId",
                table: "DeliveryOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_TenantId_OrderNumber",
                table: "DeliveryOrders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardBlocks_TenantId",
                table: "YardBlocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_YardBlocks_TenantId_BlockCode",
                table: "YardBlocks",
                columns: new[] { "TenantId", "BlockCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_BlockId",
                table: "YardSlots",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_TenantId",
                table: "YardSlots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_TenantId_BlockId_Bay_Row_Tier",
                table: "YardSlots",
                columns: new[] { "TenantId", "BlockId", "Bay", "Row", "Tier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_TenantId_IsOccupied",
                table: "YardSlots",
                columns: new[] { "TenantId", "IsOccupied" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerVisits");

            migrationBuilder.DropTable(
                name: "DeliveryOrderItems");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "YardSlots");

            migrationBuilder.DropTable(
                name: "DeliveryOrders");

            migrationBuilder.DropTable(
                name: "YardBlocks");
        }
    }
}
