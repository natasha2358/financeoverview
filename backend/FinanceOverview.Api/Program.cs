using FinanceOverview.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

await SeedData.EnsureSeededAsync(app.Services);

app.Run();
