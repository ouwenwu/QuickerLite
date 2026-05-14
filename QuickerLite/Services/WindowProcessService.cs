using System;
using System.Diagnostics;

namespace QuickerLite.Services;

public sealed class WindowProcessService
{
    public string GetProcessNameAt(int x, int y)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.Point { X = x, Y = y });
        if (hwnd == IntPtr.Zero)
        {
            return "";
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (root != IntPtr.Zero)
        {
            hwnd = root;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return "";
        }

        try
        {
            return Process.GetProcessById((int)processId).ProcessName + ".exe";
        }
        catch
        {
            return "";
        }
    }
}
