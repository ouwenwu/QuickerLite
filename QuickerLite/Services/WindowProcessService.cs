using System;
using System.Diagnostics;
using System.Text;

namespace QuickerLite.Services;

public sealed class WindowProcessService
{
    public string GetProcessNameAt(int x, int y)
    {
        var processId = GetProcessIdAt(x, y);
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

    public string GetProcessExecutablePathAt(int x, int y)
    {
        var processId = GetProcessIdAt(x, y);
        if (processId == 0 || processId == Environment.ProcessId)
        {
            return "";
        }

        var processHandle = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return "";
        }

        try
        {
            var builder = new StringBuilder(1024);
            var size = (uint)builder.Capacity;
            return NativeMethods.QueryFullProcessImageNameW(processHandle, 0, builder, ref size)
                ? builder.ToString(0, (int)size)
                : "";
        }
        catch
        {
            return "";
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }
    }

    private static uint GetProcessIdAt(int x, int y)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.Point { X = x, Y = y });
        if (hwnd == IntPtr.Zero)
        {
            return 0;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (root != IntPtr.Zero)
        {
            hwnd = root;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId;
    }
}
