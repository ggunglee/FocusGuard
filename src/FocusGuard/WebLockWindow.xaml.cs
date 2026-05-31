using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using FocusGuard.Services;

namespace FocusGuard
{
    public partial class WebLockWindow : Window
    {
        private static int _softWindowCount = 0;

        private string _targetUrl;
        private string _allowedDomain;
        private bool _strictLock;
        private bool _compactTop;
        private DispatcherTimer _compactTopTimer;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

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

        public bool IsStrictLock
        {
            get { return _strictLock; }
        }

        public bool IsCompactTop
        {
            get { return _compactTop; }
        }

        public bool IsAuthorizedToClose { get; set; } = false;

        public WebLockWindow(string url) : this(url, true, false)
        {
        }

        public WebLockWindow(string url, bool strictLock) : this(url, strictLock, false)
        {
        }

        public WebLockWindow(string url, bool strictLock, bool compactTop)
        {
            InitializeComponent();

            _strictLock = strictLock;
            _compactTop = compactTop;
            _targetUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://" + url;
            _allowedDomain = new Uri(_targetUrl).Host;

            ApplyWindowMode();

            SourceInitialized += (s, e) =>
            {
                if (_compactTop)
                {
                    KeepCompactTopMost();
                }
            };

            ContentRendered += (s, e) =>
            {
                if (_compactTop)
                {
                    KeepCompactTopMost();
                    StartCompactTopTimer();
                }
            };

            InitializeAsync();

            if (_strictLock && !_compactTop)
            {
                KeyboardBlocker.Start();

                this.StateChanged += (s, e) =>
                {
                    if (this.WindowState == WindowState.Minimized)
                    {
                        this.WindowState = WindowState.Maximized;
                        this.Activate();
                        this.Topmost = true;
                    }
                };
            }
        }

        private void ApplyWindowMode()
        {
            if (_compactTop)
            {
                var area = SystemParameters.WorkArea;

                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.ToolWindow;
                this.Topmost = true;
                this.ShowInTaskbar = true;
                this.ResizeMode = ResizeMode.CanResize;
                this.Width = 400;
                this.Height = 600;
                this.Left = area.Right - this.Width - 20;
                this.Top = area.Top + 20;
                this.Title = "📚 기록 사이트 - " + _allowedDomain;
                return;
            }

            if (_strictLock)
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
                this.Topmost = true;
                this.ShowInTaskbar = false;
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                // 일반 공부용 웹뷰도 전체화면으로 실행하여 화면 전체를 덮도록 하되,
                // 허용된 메모장 등이 앞으로 올 수 있게 Topmost는 false로 지정합니다.
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
                this.Topmost = false;
                this.ShowInTaskbar = true;
                this.ResizeMode = ResizeMode.NoResize;
            }
        }

        private void StartCompactTopTimer()
        {
            if (!_compactTop) return;
            if (_compactTopTimer != null) return;

            _compactTopTimer = new DispatcherTimer();
            _compactTopTimer.Interval = TimeSpan.FromMilliseconds(700);
            _compactTopTimer.Tick += (s, e) =>
            {
                if (IsVisible && WindowState != WindowState.Minimized)
                {
                    KeepCompactTopMost();
                }
            };
            _compactTopTimer.Start();
        }

        public void KeepCompactTopMost()
        {
            if (!_compactTop) return;

            try
            {
                // 포커스를 빼앗지 않고 HWND_TOPMOST로 재배치 (드래그 포커스가 깨지지 않도록 WPF Topmost 토글은 제거)
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(
                        hwnd,
                        HWND_TOPMOST,
                        0,
                        0,
                        0,
                        0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
                    );
                }
            }
            catch
            {
            }
        }

        public void SetStrictLockTopmost(bool topmost)
        {
            if (_strictLock && !_compactTop)
            {
                try
                {
                    this.Topmost = topmost;
                }
                catch { }
            }
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate(_targetUrl);

            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
            };

            webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                try
                {
                    Uri uri = new Uri(e.Uri);
                    if (!uri.Host.Contains(_allowedDomain))
                    {
                        e.Cancel = true;
                        MessageBox.Show("집중 모드 중에는 등록한 사이트 밖으로 이동할 수 없습니다!", "차단됨", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch
                {
                    e.Cancel = true;
                }
            };
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsAuthorizedToClose)
            {
                e.Cancel = true;
                return;
            }

            if (_compactTopTimer != null)
            {
                _compactTopTimer.Stop();
                _compactTopTimer = null;
            }

            if (_strictLock && !_compactTop)
            {
                KeyboardBlocker.Stop();
            }
        }
    }
}
