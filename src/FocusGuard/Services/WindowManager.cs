using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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
    }
}
