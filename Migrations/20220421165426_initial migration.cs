using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityFrameworkInvertedTable.Migrations
{
    public partial class initialmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityWithInvertedTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityWithInvertedTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityWithJsons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Values = table.Column<string>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityWithJsons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityWithInvertedTableId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomValues_EntityWithInvertedTables_EntityWithInvertedTableId",
                        column: x => x.EntityWithInvertedTableId,
                        principalTable: "EntityWithInvertedTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomValues_EntityWithInvertedTableId",
                table: "CustomValues",
                column: "EntityWithInvertedTableId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomValues");

            migrationBuilder.DropTable(
                name: "EntityWithJsons");

            migrationBuilder.DropTable(
                name: "EntityWithInvertedTables");
        }
    }
}
