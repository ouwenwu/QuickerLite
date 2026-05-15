using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuickerLite.Services;

public sealed class WindowTopMostService
{
    private readonly HashSet<IntPtr> pinnedWindows = [];

    public WindowTopMostResult ToggleAt(int x, int y)
    {
        var hwnd = GetRootWindowAt(x, y);
        if (hwnd == IntPtr.Zero)
        {
            return new WindowTopMostResult(WindowTopMostResultKind.NotFound, "未能识别该位置的窗口。");
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return new WindowTopMostResult(WindowTopMostResultKind.NotFound, "未能识别该窗口所属进程。");
        }

        if (processId == Environment.ProcessId)
        {
            return new WindowTopMostResult(WindowTopMostResultKind.SelfWindow, "不能选择 Quicker Lite 自己的窗口。");
        }

        if (!NativeMethods.IsWindow(hwnd))
        {
            pinnedWindows.Remove(hwnd);
            return new WindowTopMostResult(WindowTopMostResultKind.NotFound, "该窗口已经不存在。");
        }

        var isPinned = pinnedWindows.Contains(hwnd);
        var insertAfter = isPinned ? NativeMethods.HwndNoTopMost : NativeMethods.HwndTopMost;
        var success = NativeMethods.SetWindowPos(
            hwnd,
            insertAfter,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);

        if (!success)
        {
            if (!NativeMethods.IsWindow(hwnd))
            {
                pinnedWindows.Remove(hwnd);
            }

            return new WindowTopMostResult(
                WindowTopMostResultKind.Failed,
                "窗口置顶操作失败。" + GetLastWin32ErrorMessage());
        }

        if (isPinned)
        {
            pinnedWindows.Remove(hwnd);
            return new WindowTopMostResult(WindowTopMostResultKind.Unpinned, "已取消置顶。");
        }

        pinnedWindows.Add(hwnd);
        return new WindowTopMostResult(WindowTopMostResultKind.Pinned, "窗口已置顶。");
    }

    private static IntPtr GetRootWindowAt(int x, int y)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.Point { X = x, Y = y });
        if (hwnd == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        return root == IntPtr.Zero ? hwnd : root;
    }

    private static string GetLastWin32ErrorMessage()
    {
        var error = Marshal.GetLastWin32Error();
        return error == 0 ? "" : $" 错误码：{error}";
    }
}

public sealed record WindowTopMostResult(WindowTopMostResultKind Kind, string Message);

public enum WindowTopMostResultKind
{
    Pinned,
    Unpinned,
    NotFound,
    SelfWindow,
    Failed,
    Canceled
}
