#include "pch.h"
#include "main.h"
#include <Windows.h>

namespace
{
    DWORD uiThreadId;
}

namespace Kwerty::DviZe::Win
{
    public ref class MessagePumpNativeExtensions abstract sealed
    {
    public:
        static void SetUIThread()
        {
            uiThreadId = GetCurrentThreadId();
        }

        static bool Run()
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

        static void Wake()
        {
            if (PostThreadMessage(uiThreadId, WM_APP, 0, 0) == 0)
            {
                throw Win32ExceptionExtensions::FromError(NAMEOF(PostThreadMessage), GetLastError());
            }
        }
    };
}