# DviZe.Win.Common

Provides a high-performance native Win32 message pump for console and non-UI applications, with seamless `async/await` integration.

Engineered for low latency. Well-suited for use with keyboard and mouse hooks, where every tick counts.

Targets .NET 10. Written in C# and C++/CLI.

```csharp
using Kwerty.DviZe.Win;

public static async Task Main(string[] args)
{
    // Install the message pump on the current thread.
    // Execution resumes from inside the pump on the same thread.
    // 'using' ensures the pump is disposed at the end of Main, after all async work is done.
    using var messagePump = await MessagePump.Install();

    await messagePump.ThreadAccessor.ThreadPool;

    // Now on thread pool.

    await messagePump.ThreadAccessor.UIThread;

    // Now on UI thread.
}
```

`HiddenWindow` creates a hidden Win32 window for receiving specific window messages without slowing down the pump for other messages.

```csharp
const uint WM_DEVICECHANGE = 0x0219;
const uint DBT_DEVNODES_CHANGED = 0x0007;

var options = new HiddenWindowOptions { ClassName = "MyClassName", WindowName = "MyWindowName" };

await using var hiddenWindow = new HiddenWindow(options, messagePump.ThreadAccessor, loggerFactory);

await hiddenWindow.InstallAsync();

var subscription = await hiddenWindow.SubscribeAsync(WM_DEVICECHANGE, evt =>
{
    // Broadcast message triggered by plugging/unplugging a USB device.
    if ((uint)evt.WParam == DBT_DEVNODES_CHANGED)
    {
        Console.WriteLine("Device change event.");
    }
});

var hwnd = hiddenWindow.Hwnd.Value; // The window handle.

subscription.Dispose(); // Unsubscribe.
```

## Dependency Injection

For a more structured example with dependency injection see [ExampleApp1](examples/ExampleApp1/).
