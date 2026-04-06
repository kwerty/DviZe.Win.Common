#include "pch.h"
#include "HiddenWindowNativeExtensions.h"
#include "main.h"
#include <msclr/gcroot.h>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <optional>
#include <string>
#include <utility>
#include <vector>
#include <Windows.h>
#include <unordered_map>

using namespace msclr;
using namespace System;
using namespace Kwerty::DviZe::Win;

struct HiddenWindowHandler
{
	HWND hwnd;
	std::optional<UINT> msg;
	gcroot<HiddenWindowCallback^> callback;
};

namespace
{
	unsigned int nextHandlerId;
	std::unordered_map<unsigned int, HiddenWindowHandler> handlers;
	std::vector<const HiddenWindowHandler*> fastIter;

	LRESULT WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
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

			auto result = handler->callback->Invoke(
				IntPtr((void*)hWnd),
				msg,
				IntPtr((void*)wParam),
				IntPtr((void*)lParam)
			);

			if (result.HasValue)
			{
				return (LRESULT)result.Value.ToPointer();
			}
		}

		return DefWindowProc(hWnd, msg, wParam, lParam);
	}
}

IntPtr HiddenWindowNativeExtensions::CreateHiddenWindow(String^ className, String^ windowName)
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

void HiddenWindowNativeExtensions::DestroyHiddenWindow(IntPtr hwnd, String^ className)
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

UInt32 HiddenWindowNativeExtensions::RegisterHandler(IntPtr hwnd, Nullable<UInt32> msg, HiddenWindowCallback^ callback)
{
	auto hwndNative = reinterpret_cast<HWND>(hwnd.ToPointer());
	auto msgNative = msg.HasValue ? std::optional<UINT>(msg.Value) : std::nullopt;

	auto handlerId = ++nextHandlerId;

	handlers[handlerId] = HiddenWindowHandler
	{
		.hwnd = hwndNative,
		.msg = msgNative,
		.callback = gcroot<HiddenWindowCallback^>(callback)
	};

	fastIter.push_back(&handlers[handlerId]);

	return handlerId;
}

void HiddenWindowNativeExtensions::UnregisterHandler(UInt32 id)
{
	auto pair = handlers.find(id);
	if (pair != handlers.end())
	{
		std::erase(fastIter, &pair->second);
		handlers.erase(pair);
	}
}
