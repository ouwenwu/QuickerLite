using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace QuickerLite.Services;

public sealed class MouseHookService : IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly NativeMethods.LowLevelMouseProc hookCallback;
    private IntPtr hookHandle;
    private bool disposed;

    public MouseHookService(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        hookCallback = HookCallback;
    }

    public event EventHandler<MiddleClickEventArgs>? MiddleClicked;
    public event EventHandler<MiddleClickEventArgs>? MiddleReleased;

    public Func<int, int, bool>? ShouldSuppressMiddleClick { get; set; }

    public void Start()
    {
        if (hookHandle != IntPtr.Zero)
        {
            return;
        }

        hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, hookCallback, IntPtr.Zero, 0);
        if (hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法安装全局鼠标钩子。");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == NativeMethods.WmMButtonDown || wParam == NativeMethods.WmMButtonUp))
        {
            var info = Marshal.PtrToStructure<NativeMethods.Msllhookstruct>(lParam);
            var shouldSuppress = true;
            try
            {
                shouldSuppress = ShouldSuppressMiddleClick?.Invoke(info.Pt.X, info.Pt.Y) ?? true;
            }
            catch
            {
                shouldSuppress = true;
            }

            if (!shouldSuppress)
            {
                return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
            }

            dispatcher.BeginInvoke(() =>
            {
                if (wParam == NativeMethods.WmMButtonDown)
                {
                    MiddleClicked?.Invoke(this, new MiddleClickEventArgs(info.Pt.X, info.Pt.Y));
                }
                else
                {
                    MiddleReleased?.Invoke(this, new MiddleClickEventArgs(info.Pt.X, info.Pt.Y));
                }
            });

            return new IntPtr(1);
        }

        return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
    }
}

public sealed class MiddleClickEventArgs(int x, int y) : EventArgs
{
    public int X { get; } = x;
    public int Y { get; } = y;
}
