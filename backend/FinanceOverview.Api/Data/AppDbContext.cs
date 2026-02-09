using FinanceOverview.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Data;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<StagedTransaction> StagedTransactions => Set<StagedTransaction>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = _configuration.GetConnectionString("Default")
            ?? "Data Source=financeoverview.db";

        optionsBuilder.UseSqlite(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ImportBatch>()
            .Property(batch => batch.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ImportBatch>()
            .HasIndex(batch => new { batch.StatementMonth, batch.Sha256Hash })
            .IsUnique();

        modelBuilder.Entity<StagedTransaction>()
            .HasOne(staged => staged.ImportBatch)
            .WithMany(batch => batch.StagedTransactions)
            .HasForeignKey(staged => staged.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StagedTransaction>()
            .Property(staged => staged.IsApproved)
            .HasDefaultValue(false);

        modelBuilder.Entity<Transaction>()
            .HasIndex(transaction => new { transaction.ImportBatchId, transaction.RowFingerprint })
            .IsUnique()
            .HasFilter("ImportBatchId IS NOT NULL AND RowFingerprint IS NOT NULL");
    }
}
