using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddTransactionImportFingerprint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ImportBatchId",
            table: "Transactions",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RowFingerprint",
            table: "Transactions",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ImportBatchId_RowFingerprint",
            table: "Transactions",
            columns: new[] { "ImportBatchId", "RowFingerprint" },
            unique: true,
            filter: "ImportBatchId IS NOT NULL AND RowFingerprint IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Transactions_ImportBatchId_RowFingerprint",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "ImportBatchId",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "RowFingerprint",
            table: "Transactions");
    }
}
