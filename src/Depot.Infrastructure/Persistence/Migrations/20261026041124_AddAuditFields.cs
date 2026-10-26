using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "YardRows",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "YardRows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "YardRows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "YardRows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "LineOperators",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LineOperators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "LineOperators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "LineOperators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ContainerTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ContainerTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "ContainerTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "ContainerTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Blocks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Blocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Blocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Blocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Bays",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Bays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Bays",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Bays",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    LineOperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    SizeFeet = table.Column<int>(type: "integer", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TareWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Containers_LineOperators_LineOperatorId",
                        column: x => x.LineOperatorId,
                        principalTable: "LineOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0001-4a5e-9a10-000000000001"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0002-4a5e-9a10-000000000002"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0003-4a5e-9a10-000000000003"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0004-4a5e-9a10-000000000004"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0005-4a5e-9a10-000000000005"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0006-4a5e-9a10-000000000006"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.UpdateData(
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: new Guid("5b0c6f10-0007-4a5e-9a10-000000000007"),
                columns: new[] { "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Containers_ContainerTypeId",
                table: "Containers",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_LineOperatorId",
                table: "Containers",
                column: "LineOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_Number",
                table: "Containers",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "YardRows");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "YardRows");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "YardRows");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "YardRows");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LineOperators");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LineOperators");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "LineOperators");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "LineOperators");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ContainerTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ContainerTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "ContainerTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ContainerTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Bays");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bays");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Bays");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Bays");
        }
    }
}
