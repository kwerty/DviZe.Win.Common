using System;

namespace Kwerty.DviZe.Win;

public sealed class HiddenWindowEvent(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
{
    public IntPtr Hwnd => hwnd;

    public uint Msg => msg;

    public IntPtr WParam => wParam;

    public IntPtr LParam => lParam;

    public IntPtr? ReturnValue { get; set; }
}
