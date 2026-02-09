using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddStagedTransactions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StagedTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                RowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                BookingDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                ValueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                RawDescription = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                Currency = table.Column<string>(type: "TEXT", nullable: true),
                RunningBalance = table.Column<decimal>(type: "TEXT", nullable: true),
                IsApproved = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StagedTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_StagedTransactions_ImportBatches_ImportBatchId",
                    column: x => x.ImportBatchId,
                    principalTable: "ImportBatches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StagedTransactions_ImportBatchId",
            table: "StagedTransactions",
            column: "ImportBatchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StagedTransactions");
    }
}
