using FinanceOverview.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceOverview.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250202000100_AddImportBatchUniqueIndex")]
partial class AddImportBatchUniqueIndex
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.0");

        modelBuilder.Entity("FinanceOverview.Api.Models.ImportBatch", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<string>("OriginalFileName")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("Sha256Hash")
                    .HasColumnType("TEXT");

                b.Property<DateOnly>("StatementMonth")
                    .HasColumnType("TEXT");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("StorageKey")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<DateTime>("UploadedAt")
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.HasIndex("StatementMonth", "Sha256Hash")
                    .IsUnique();

                b.ToTable("ImportBatches");
            });

        modelBuilder.Entity("FinanceOverview.Api.Models.Transaction", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<decimal>("Amount")
                    .HasColumnType("TEXT");

                b.Property<decimal?>("Balance")
                    .HasColumnType("TEXT");

                b.Property<string>("Currency")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<DateOnly>("Date")
                    .HasColumnType("TEXT");

                b.Property<string>("Merchant")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("RawDescription")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("Transactions");
            });
    }
}
