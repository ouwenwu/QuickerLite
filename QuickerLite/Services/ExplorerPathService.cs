using System;
using System.IO;

namespace QuickerLite.Services;

public sealed class ExplorerPathService
{
    public string? GetFolderPathAt(int screenX, int screenY)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.Point { X = screenX, Y = screenY });
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (root != IntPtr.Zero)
        {
            hwnd = root;
        }

        return GetFolderPathForExplorerWindow(hwnd);
    }

    private static string? GetFolderPathForExplorerWindow(IntPtr hwnd)
    {
        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
        {
            return null;
        }

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell is null)
        {
            return null;
        }

        foreach (dynamic window in shell.Windows())
        {
            try
            {
                if ((IntPtr)(int)window.HWND != hwnd)
                {
                    continue;
                }

                string url = window.LocationURL;
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var path = Uri.UnescapeDataString(new Uri(url).LocalPath);
                return Directory.Exists(path) ? path : null;
            }
            catch
            {
            }
        }

        return null;
    }
}
