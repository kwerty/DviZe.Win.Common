#pragma once

using namespace System;

namespace Kwerty::DviZe::Win
{
	private delegate Nullable<IntPtr> HiddenWindowCallback(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

	private ref class HiddenWindowNativeExtensions abstract sealed
	{
	public:
		static IntPtr CreateHiddenWindow(String^ className, String^ windowName);

		static void DestroyHiddenWindow(IntPtr hwnd, String^ className);

		static UInt32 RegisterHandler(IntPtr hwnd, Nullable<UInt32> msg, HiddenWindowCallback^ callback);

		static void UnregisterHandler(UInt32 id);
	};
}


