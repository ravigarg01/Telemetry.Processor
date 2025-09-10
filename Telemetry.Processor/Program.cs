using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;
using Telemetry.Processor;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure DbContext
        var dbConnectionString = hostContext.Configuration.GetConnectionString("TelemetryDb");
        services.AddDbContext<TelemetryDbContext>(options =>
            options.UseSqlServer(dbConnectionString));

        // Configure Service Bus
        var serviceBusConnectionString = hostContext.Configuration["ServiceBusConnectionString"];
        services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));

        // Add the background worker
        services.AddHostedService<TelemetryWorker>();
    })
    .Build();

// Create the database if it doesn't exist
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    db.Database.EnsureCreated();
}

await host.RunAsync();