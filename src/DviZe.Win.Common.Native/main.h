#pragma once

#define NAMEOF(name) #name

using namespace System;

namespace Kwerty::DviZe::Win
{
	public ref class HiddenWindowEvent sealed
	{
	private:
		initonly IntPtr hwnd;
		initonly UInt32 msg;
		initonly IntPtr wParam;
		initonly IntPtr lParam;

	public:
		HiddenWindowEvent(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam)
			: hwnd(hwnd), msg(msg), wParam(wParam), lParam(lParam)
		{
		}

		property IntPtr Hwnd
		{
			IntPtr get()
			{
				return hwnd;
			}
		}

		property UInt32 Msg
		{
			UInt32 get()
			{
				return msg;
			}
		}

		property IntPtr WParam
		{
			IntPtr get()
			{
				return wParam;
			}
		}

		property IntPtr LParam
		{
			IntPtr get()
			{
				return lParam;
			}
		}

		property Nullable<IntPtr> ReturnValue;
	};
}
