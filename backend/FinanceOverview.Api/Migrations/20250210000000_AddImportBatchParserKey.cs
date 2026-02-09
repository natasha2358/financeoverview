using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddImportBatchParserKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ParserKey",
            table: "ImportBatches",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ParserKey",
            table: "ImportBatches");
    }
}
