#pragma once

#define NAMEOF(name) #name

namespace Kwerty::DviZe::Win
{
	public ref class HiddenWindowEvent sealed
	{
	private:
		initonly System::IntPtr hwnd;
		initonly System::UInt32 msg;
		initonly System::IntPtr wParam;
		initonly System::IntPtr lParam;

	public:
		HiddenWindowEvent(System::IntPtr hwnd, System::UInt32 msg, System::IntPtr wParam, System::IntPtr lParam)
			: hwnd(hwnd), msg(msg), wParam(wParam), lParam(lParam)
		{
		}

		property System::IntPtr Hwnd
		{
			System::IntPtr get()
			{
				return hwnd;
			}
		}

		property System::UInt32 Msg
		{
			System::UInt32 get()
			{
				return msg;
			}
		}

		property System::IntPtr WParam
		{
			System::IntPtr get()
			{
				return wParam;
			}
		}

		property System::IntPtr LParam
		{
			System::IntPtr get()
			{
				return lParam;
			}
		}

		property System::Nullable<System::IntPtr> ReturnValue;
	};
}
