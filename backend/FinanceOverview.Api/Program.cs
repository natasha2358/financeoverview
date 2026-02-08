using FinanceOverview.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    var shouldSeedDemoData = app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("SeedDemoData");

    if (shouldSeedDemoData)
    {
        await SeedData.EnsureSeededAsync(app.Services);
    }
}

app.Run();

public partial class Program { }
