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
}
