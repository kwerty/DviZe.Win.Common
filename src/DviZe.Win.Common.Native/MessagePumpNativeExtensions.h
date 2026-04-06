#pragma once

namespace Kwerty::DviZe::Win
{
	private ref class MessagePumpNativeExtensions abstract sealed
	{
	public:
		static void SetUIThread();

		static bool Run();

		static void Wake();
	};
}
