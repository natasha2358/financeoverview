using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddImportBatches : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImportBatches",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                StatementMonth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                StorageKey = table.Column<string>(type: "TEXT", nullable: false),
                Sha256Hash = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportBatches", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImportBatches");
    }
}
