using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Depot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ContainerTypes",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("5b0c6f10-0001-4a5e-9a10-000000000001"), "DRY", "Dry" },
                    { new Guid("5b0c6f10-0002-4a5e-9a10-000000000002"), "REEFER", "Reefer" },
                    { new Guid("5b0c6f10-0003-4a5e-9a10-000000000003"), "OPENTOP", "Open Top" },
                    { new Guid("5b0c6f10-0004-4a5e-9a10-000000000004"), "FLATRACK", "Flat Rack" },
                    { new Guid("5b0c6f10-0005-4a5e-9a10-000000000005"), "BUNKER", "Bunker" },
                    { new Guid("5b0c6f10-0006-4a5e-9a10-000000000006"), "VENTILATED", "Ventilated" },
                    { new Guid("5b0c6f10-0007-4a5e-9a10-000000000007"), "SPECIALIZED", "Specialized" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTypes_Code",
                table: "ContainerTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerTypes");
        }
    }
}
