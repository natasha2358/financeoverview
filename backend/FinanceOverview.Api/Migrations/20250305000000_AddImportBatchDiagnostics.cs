using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddImportBatchDiagnostics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "FirstBookingDate",
            table: "ImportBatches",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "LastBookingDate",
            table: "ImportBatches",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ParsedRowCount",
            table: "ImportBatches",
            type: "INTEGER",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FirstBookingDate",
            table: "ImportBatches");

        migrationBuilder.DropColumn(
            name: "LastBookingDate",
            table: "ImportBatches");

        migrationBuilder.DropColumn(
            name: "ParsedRowCount",
            table: "ImportBatches");
    }
}
