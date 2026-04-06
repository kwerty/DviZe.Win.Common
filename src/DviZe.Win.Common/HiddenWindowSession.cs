using Kwerty.DviZe.Workers;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Kwerty.DviZe.Win;

internal sealed class HiddenWindowSession(HiddenWindowOptions options, IThreadAccessor threadAccessor, ILoggerFactory loggerFactory) : Worker
{
    const int ERROR_CLASS_ALREADY_EXISTS = 1410;
    readonly ILogger logger = loggerFactory.CreateLogger<HiddenWindowSession>();
    internal IntPtr hwnd;

    protected override async Task OnStartingAsync(WorkerStartingContext startingContext)
    {
        await threadAccessor.UIThread;

        try
        {
            hwnd = HiddenWindowNativeExtensions.CreateHiddenWindow(options.ClassName, options.WindowName);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CLASS_ALREADY_EXISTS)
        {
            throw new InvalidOperationException("Class name already in use.", ex);
        }
        
        logger.LogDebug("Class Name: {ClassName}, Window Name: {WindowName}, Hwnd: 0x{hwnd:X}", options.ClassName, options.WindowName, hwnd);
    }

    protected override async Task OnStoppingAsync()
    {
        await threadAccessor.UIThread;

        HiddenWindowNativeExtensions.DestroyHiddenWindow(hwnd, options.ClassName);
    }
}
