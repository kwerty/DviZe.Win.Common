#include "pch.h"
#include "main.h"
#include "MessagePumpNativeExtensions.h"
#include <Windows.h>

using namespace Kwerty::DviZe::Win;

namespace
{
    DWORD uiThreadId;
}

void MessagePumpNativeExtensions::SetUIThread()
{
    uiThreadId = GetCurrentThreadId();
}

bool MessagePumpNativeExtensions::Run()
{
    while (true)
    {
        MSG msg;
        auto result = GetMessage(&msg, NULL, 0, 0);

        if (result == 0)
        {
            return false;
        }

        if (result < 0)
        {
            throw Win32ExceptionExtensions::FromError(NAMEOF(GetMessage), GetLastError());
        }

        if (msg.message == WM_APP) // Wake.
        {
            return true;
        }

        DispatchMessage(&msg);
    }
}

void MessagePumpNativeExtensions::Wake()
{
    if (PostThreadMessage(uiThreadId, WM_APP, 0, 0) == 0)
    {
        throw Win32ExceptionExtensions::FromError(NAMEOF(PostThreadMessage), GetLastError());
    }
}
