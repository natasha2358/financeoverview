using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddImportBatchUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ImportBatches_StatementMonth_Sha256Hash",
            table: "ImportBatches",
            columns: new[] { "StatementMonth", "Sha256Hash" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ImportBatches_StatementMonth_Sha256Hash",
            table: "ImportBatches");
    }
}
