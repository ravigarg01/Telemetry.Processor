using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Telemetry.Processor
{
    public class TelemetryWorker : IHostedService
    {
        private readonly ILogger<TelemetryWorker> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IServiceProvider _serviceProvider;
        private ServiceBusProcessor _processor;
        private const string QueueName = "telemetry-queue";

        public TelemetryWorker(ILogger<TelemetryWorker> logger, ServiceBusClient serviceBusClient, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _processor = _serviceBusClient.CreateProcessor(QueueName, new ServiceBusProcessorOptions());
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;
            await _processor.StartProcessingAsync(cancellationToken);
            _logger.LogInformation("Telemetry processor started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry processor stopping.");
            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            _logger.LogInformation("Received message: {body}", body);

            var telemetryData = JsonSerializer.Deserialize<TelemetryData>(body);

            if (telemetryData != null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
                    dbContext.Telemetry.Add(telemetryData);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Saved data for {DeviceId} to database.", telemetryData.DeviceId);
                }
            }

            // Complete the message. This removes it from the queue.
            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Message handler encountered an exception");
            return Task.CompletedTask;
        }
    }
}