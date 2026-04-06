using Kwerty.DviZe.Win;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleApp1;

public class HiddenWindowExample(HiddenWindow hiddenWindow, ILogger<HiddenWindowExample> logger) : IHostedService
{
    const uint WM_DEVICECHANGE = 0x0219;
    const uint DBT_DEVNODES_CHANGED = 0x0007;
    IDisposable hiddenWindowSubscription;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        hiddenWindowSubscription = await hiddenWindow.SubscribeAsync(WM_DEVICECHANGE, evt =>
        {
            // Broadcast message triggered by plugging/unplugging a USB device.
            if ((uint)evt.WParam == DBT_DEVNODES_CHANGED)
            {
                logger.LogInformation("Device change event.");
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        hiddenWindowSubscription.Dispose();
        return Task.CompletedTask;
    }
}
