using Kwerty.DviZe.Win;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ExampleApp1;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Must be installed first and disposed last.
        // The current thread will become the UI thread.
        using var messagePump = await MessagePump.Install();

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder
                .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None)
                .AddSimpleConsole(opts => opts.SingleLine = true);
        });

        builder.Services.AddSingleton(messagePump.ThreadAccessor);

        builder.Services.AddSingleton(new HiddenWindowOptions { ClassName = "MyClassName", WindowName = "MyWindowName" });
        builder.Services.AddSingleton<HiddenWindow>();

        builder.Services.AddHostedService<ThreadSwitchingExample>();
        builder.Services.AddHostedService<HiddenWindowExample>();

        var host = builder.Build();

        // Restores legacy CTRL_CLOSE_EVENT handling on Windows, which was removed with .NET 10.
        // Without it, closing the console kills the process immediately, bypassing graceful shutdown.
        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler
        using var closeHandler = PosixSignalRegistration.Create(PosixSignal.SIGHUP, _ =>
        {
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.StopApplication();
            lifetime.ApplicationStopped.WaitHandle.WaitOne();
        });

        var hiddenWindow = host.Services.GetRequiredService<HiddenWindow>();
        await hiddenWindow.InstallAsync();

        await host.RunAsync();
    }
}
