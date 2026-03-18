using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionsIngest.App.Data;
using TransactionsIngest.App.Services;

// Build configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Set up dependency injection
var services = new ServiceCollection();

// Add DbContext with SQLite
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// add the mockfetcher
var useMock=configuration.GetValue<bool>("ApiSettings:UseMock");
if (useMock)
    services.AddScoped<ITransactionFetcher, MockTransactionFetcher>();
else
    throw new InvalidOperationException("Only mock fetcher is supported currently");

// Register ingestion
services.AddScoped<IngestionService>();
var serviceProvider=services.BuildServiceProvider();
// Run the ingestion
using var scope=serviceProvider.CreateScope();
var ingestionService=scope.ServiceProvider.GetRequiredService<IngestionService>();
await ingestionService.RunAsync();