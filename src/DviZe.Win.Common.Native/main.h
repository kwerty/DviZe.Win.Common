#pragma once

#define NAMEOF(name) #name

using namespace System;

namespace Kwerty::DviZe::Win
{
	public delegate Nullable<IntPtr> HiddenWindowCallback(IntPtr hwnd, UInt32 msg, IntPtr wParam, IntPtr lParam);
}
