using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TennisBookings.ResultsProcessing;
using System.IO;

namespace TennisBookings.Web.BackgroundServices
{
    public class FileProcessingService : BackgroundService
    {
        private readonly ILogger<FileProcessingService> _logger;
        private readonly FileProcessingChannel _channel;
        private readonly IServiceProvider _serviceProvider;

        public FileProcessingService(ILogger<FileProcessingService> logger, FileProcessingChannel boundedMessageChannel,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _channel = boundedMessageChannel;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach(var fileName in _channel.ReadAllAsync())
            {
                using var scope = _serviceProvider.CreateScope();

                var processor = scope.ServiceProvider.GetRequiredService<IResultProcessor>();

                try
                {
                    await using var stream = File.OpenRead(fileName);

                    await processor.ProcessAsync(stream);
                }
                finally
                {
                    File.Delete(fileName); // Delete the temp file
                }
            }
        }
    }
}
