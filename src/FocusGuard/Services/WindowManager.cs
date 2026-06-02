using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FocusGuard.Services
{
    public class OpenWindowInfo
    {
        public string ProcessName { get; set; }
        public string Title { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Title) ? ProcessName : $"[{ProcessName}] {Title}";
        }
    }

    public class WindowManager
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        public class MonitorInfo
        {
            public RECT Bounds { get; set; }
            public RECT WorkingArea { get; set; }
            public bool IsPrimary { get; set; }
        }

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const int SW_RESTORE = 9;

        public bool IsTargetWindowActive(string targetProcessName, string targetTitleKeyword = null)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            
            try
            {
                Process activeProcess = Process.GetProcessById((int)processId);
                bool isProcessMatch = activeProcess.ProcessName.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase);
                if (!isProcessMatch) return false;

                if (!string.IsNullOrEmpty(targetTitleKeyword))
                {
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(foregroundWindow, windowText, 256);
                    return windowText.ToString().Contains(targetTitleKeyword, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void BringTargetToForeground(string targetProcessName)
        {
            Process[] processes = Process.GetProcessesByName(targetProcessName);
            if (processes.Length > 0)
            {
                IntPtr mainWindowHandle = processes[0].MainWindowHandle;
                if (mainWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(mainWindowHandle);
                }
            }
        }

        public List<OpenWindowInfo> GetOpenWindows()
        {
            var list = new List<OpenWindowInfo>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                    {
                        list.Add(new OpenWindowInfo
                        {
                            ProcessName = proc.ProcessName,
                            Title = proc.MainWindowTitle
                        });
                    }
                }
                catch
                {
                    // Ignore access denied exceptions for system processes
                }
            }
            return list;
        }

        public List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                var mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(new MonitorInfo
                    {
                        Bounds = mi.rcMonitor,
                        WorkingArea = mi.rcWork,
                        IsPrimary = (mi.dwFlags & 1) != 0
                    });
                }
                return true;
            }, IntPtr.Zero);

            return monitors.OrderByDescending(m => m.IsPrimary).ToList();
        }

        public void SetTargetWindowToTopmostAndSize(string targetProcessName, int left, int top, int width, int height, bool makeTopmost)
        {
            Process[] processes = Process.GetProcessesByName(targetProcessName);
            foreach (var proc in processes)
            {
                IntPtr hwnd = proc.MainWindowHandle;
                if (hwnd != IntPtr.Zero)
                {
                    ShowWindow(hwnd, SW_RESTORE);

                    IntPtr insertAfter = makeTopmost ? HWND_TOPMOST : HWND_NOTOPMOST;

                    SetWindowPos(
                        hwnd,
                        insertAfter,
                        left,
                        top,
                        width,
                        height,
                        0x0040 // SWP_SHOWWINDOW
                    );
                }
            }
        }
    }
}
