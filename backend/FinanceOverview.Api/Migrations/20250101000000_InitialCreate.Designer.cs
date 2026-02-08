using System;
using FinanceOverview.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FinanceOverview.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20250101000000_InitialCreate")]
partial class InitialCreate : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.0");

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
