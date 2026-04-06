using Kwerty.DviZe.Win;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleApp1;

public class ThreadSwitchingExample(IThreadAccessor threadAccessor, ILogger<ThreadSwitchingExample> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                logger.LogInformation("Background task on thread: {threadId}.", Environment.CurrentManagedThreadId);

                logger.LogInformation("Background task switching to UI thread.");

                await threadAccessor.UIThread;

                logger.LogInformation("Background task now on UI thread: {threadId}.", Environment.CurrentManagedThreadId);

                logger.LogInformation("Sleeping...");

                await Task.Delay(2500, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
