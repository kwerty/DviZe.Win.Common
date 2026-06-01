#include "pch.h"
#include "main.h"
#include <msclr/gcroot.h>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <optional>
#include <string>
#include <vector>
#include <Windows.h>
#include <unordered_map>

using namespace Kwerty::DviZe::Win;
using namespace msclr;
using namespace System;

namespace
{
	struct HiddenWindowHandler
	{
		HWND hwnd;
		std::optional<UINT> msg;
		gcroot<Action<HiddenWindowEvent^>^> callback;
	};

	unsigned int nextHandlerId;
	std::unordered_map<unsigned int, HiddenWindowHandler> handlers;
	std::vector<const HiddenWindowHandler*> fastIter;
}

static LRESULT WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	for (const auto* handler : fastIter)
	{
		if (handler->hwnd != hWnd)
		{
			continue;
		}

		if (handler->msg.has_value()
			&& handler->msg.value() != msg)
		{
			continue;
		}

		auto evt = gcnew HiddenWindowEvent(
			IntPtr((void*)hWnd),
			msg,
			IntPtr((void*)wParam),
			IntPtr((void*)lParam)
		);

		handler->callback->Invoke(evt);

		if (evt->ReturnValue.HasValue)
		{
			return (LRESULT)evt->ReturnValue.Value.ToPointer();
		}
	}

	return DefWindowProc(hWnd, msg, wParam, lParam);
}

static void RebuildFastIter()
{
	fastIter = {};

	for (const auto& [_, handler] : handlers)
	{
		fastIter.push_back(&handler);
	}
}

namespace Kwerty::DviZe::Win
{
	private ref class HiddenWindowNativeExtensions abstract sealed
	{
	public:
		static IntPtr CreateHiddenWindow(String^ className, String^ windowName)
		{
			auto classNameNative = msclr::interop::marshal_as<std::wstring>(className);
			auto windowNameNative = msclr::interop::marshal_as<std::wstring>(windowName);

			auto wndClass = WNDCLASSEX
			{
				.cbSize = sizeof(WNDCLASSEX),
				.lpfnWndProc = &WndProc,
				.lpszClassName = classNameNative.c_str(),
			};

			auto classHandle = RegisterClassEx(&wndClass);
			if (classHandle == 0)
			{
				throw Win32ExceptionExtensions::FromError(NAMEOF(RegisterClassEx), GetLastError());
			}

			auto windowHandle = CreateWindowEx(0, classNameNative.c_str(), windowNameNative.c_str(), 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL);
			if (windowHandle == NULL)
			{
				throw Win32ExceptionExtensions::FromError(NAMEOF(CreateWindowEx), GetLastError());
			}

			return IntPtr((void*)windowHandle);
		}

		static void DestroyHiddenWindow(IntPtr hwnd, String^ className)
		{
			auto hwndNative = reinterpret_cast<HWND>(hwnd.ToPointer());
			auto classNameNative = msclr::interop::marshal_as<std::wstring>(className);

			if (!DestroyWindow(hwndNative))
			{
				throw Win32ExceptionExtensions::FromError(NAMEOF(DestroyWindow), GetLastError());
			}

			if (!UnregisterClass(classNameNative.c_str(), NULL))
			{
				throw Win32ExceptionExtensions::FromError(NAMEOF(UnregisterClass), GetLastError());
			}
		}

		static UInt32 RegisterHandler(IntPtr hwnd, Nullable<UInt32> msg, Action<HiddenWindowEvent^>^ callback)
		{
			auto hwndNative = reinterpret_cast<HWND>(hwnd.ToPointer());
			auto msgNative = msg.HasValue ? std::optional<UINT>(msg.Value) : std::nullopt;

			auto handlerId = nextHandlerId++;

			handlers[handlerId] = HiddenWindowHandler
			{
				.hwnd = hwndNative,
				.msg = msgNative,
				.callback = gcroot<Action<HiddenWindowEvent^>^>(callback)
			};

			RebuildFastIter();

			return handlerId;
		}

		static void UnregisterHandler(UInt32 id)
		{
			handlers.erase(id);

			RebuildFastIter();
		}
	};
}