using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExportVessel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseOrders_LineOperators_LineOperatorId",
                        column: x => x.LineOperatorId,
                        principalTable: "LineOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeFeet = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseOrderItems_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseOrderItems_ReleaseOrders_ReleaseOrderId",
                        column: x => x.ReleaseOrderId,
                        principalTable: "ReleaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseOrderItems_ContainerTypeId",
                table: "ReleaseOrderItems",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseOrderItems_ReleaseOrderId_ContainerTypeId_SizeFeet",
                table: "ReleaseOrderItems",
                columns: new[] { "ReleaseOrderId", "ContainerTypeId", "SizeFeet" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseOrders_CustomerId",
                table: "ReleaseOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseOrders_LineOperatorId_OrderNo",
                table: "ReleaseOrders",
                columns: new[] { "LineOperatorId", "OrderNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseOrderItems");

            migrationBuilder.DropTable(
                name: "ReleaseOrders");
        }
    }
}
