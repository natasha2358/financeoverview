using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

public partial class AddMerchantRulesAndCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CategoryId",
            table: "Transactions",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MerchantNormalized",
            table: "Transactions",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MerchantRules",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Pattern = table.Column<string>(type: "TEXT", nullable: false),
                MatchType = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedMerchant = table.Column<string>(type: "TEXT", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                Priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MerchantRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_MerchantRules_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MerchantRules_CategoryId",
            table: "MerchantRules",
            column: "CategoryId");

        migrationBuilder.AddForeignKey(
            name: "FK_Transactions_Categories_CategoryId",
            table: "Transactions",
            column: "CategoryId",
            principalTable: "Categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Transactions_Categories_CategoryId",
            table: "Transactions");

        migrationBuilder.DropTable(
            name: "MerchantRules");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "CategoryId",
            table: "Transactions");

        migrationBuilder.DropColumn(
            name: "MerchantNormalized",
            table: "Transactions");
    }
}
