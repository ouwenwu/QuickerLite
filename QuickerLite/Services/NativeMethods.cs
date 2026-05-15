using System;
using System.Runtime.InteropServices;

namespace QuickerLite.Services;

internal static partial class NativeMethods
{
    public const int WhMouseLl = 14;
    public const int WmMButtonDown = 0x0207;
    public const int WmMButtonUp = 0x0208;
    public const int WmMouseWheel = 0x020A;
    public const int GaRoot = 2;

    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Msllhookstruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    public static partial IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hmod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    public static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    public static partial IntPtr WindowFromPoint(Point point);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
