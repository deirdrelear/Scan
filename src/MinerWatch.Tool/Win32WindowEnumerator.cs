using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using MinerWatch.Core.Windows;

namespace MinerWatch.Tool;

[SupportedOSPlatform("windows10.0.15063")]
public sealed class Win32WindowEnumerator : IWindowEnumerator
{
    public IReadOnlyList<WindowObservation> Enumerate()
    {
        var result = new List<WindowObservation>();
        // Changes only this observer thread. Physical-pixel geometry must not depend on host DPI virtualization.
        var previousDpi = SetThreadDpiAwarenessContext(new IntPtr(-4)); // PER_MONITOR_AWARE_V2
        if (previousDpi == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot establish physical-pixel geometry.");
        try
        {
            bool success = EnumWindows((hwnd, _) =>
            {
                try
                {
                    GetWindowThreadProcessId(hwnd, out var pid);
                    if (pid == 0) return true;
                    using var process = Process.GetProcessById(checked((int)pid));
                    if (!string.Equals(process.ProcessName, "exefile", StringComparison.OrdinalIgnoreCase)) return true;
                    var title = new StringBuilder(1024);
                    GetWindowTextW(hwnd, title, title.Capacity);
                    if (!title.ToString().StartsWith("EVE", StringComparison.Ordinal)) return true;
                    var className = new StringBuilder(256);
                    GetClassNameW(hwnd, className, className.Capacity);
                    var geometryValid = GetClientRect(hwnd, out var rect);
                    var cloakResult = DwmGetWindowAttribute(hwnd, 14, out var cloaked, sizeof(int));
                    result.Add(new WindowObservation(hwnd.ToInt64(), (int)pid, process.StartTime.ToUniversalTime().Ticks,
                        process.ProcessName, title.ToString(), className.ToString(), geometryValid ? rect.Right - rect.Left : 0,
                        geometryValid ? rect.Bottom - rect.Top : 0, GetDpiForWindow(hwnd), IsWindowVisible(hwnd),
                        IsIconic(hwnd), cloakResult != 0 || cloaked != 0));
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or OverflowException)
                {
                    // A disappearing/inaccessible process is omitted; a registered client becomes Missing.
                }
                return true;
            }, IntPtr.Zero);
            if (!success) throw new Win32Exception(Marshal.GetLastWin32Error(), "Window enumeration failed.");
            return result.AsReadOnly();
        }
        finally { SetThreadDpiAwarenessContext(previousDpi); }
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr parameter);
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetWindowTextW(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetClassNameW(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
}
