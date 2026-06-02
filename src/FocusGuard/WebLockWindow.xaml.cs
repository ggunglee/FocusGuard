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
        private WindowManager.RECT? _targetMonitorBounds;

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

        public WebLockWindow(string url) : this(url, true, false, null)
        {
        }

        public WebLockWindow(string url, bool strictLock) : this(url, strictLock, false, null)
        {
        }

        public WebLockWindow(string url, bool strictLock, bool compactTop) : this(url, strictLock, compactTop, null)
        {
        }

        public WebLockWindow(string url, bool strictLock, bool compactTop, WindowManager.RECT? monitorBounds)
        {
            InitializeComponent();

            _strictLock = strictLock;
            _compactTop = compactTop;
            _targetMonitorBounds = monitorBounds;
            _targetUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://" + url;
            _allowedDomain = new Uri(_targetUrl).Host;

            this.Loaded += async (s, e) =>
            {
                ApplyWindowMode();
                await InitializeAsync();
            };

            SourceInitialized += (s, e) =>
            {
                if (_compactTop && !_targetMonitorBounds.HasValue)
                {
                    KeepCompactTopMost();
                }
            };

            ContentRendered += (s, e) =>
            {
                if (_compactTop && !_targetMonitorBounds.HasValue)
                {
                    KeepCompactTopMost();
                    StartCompactTopTimer();
                }
            };

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
            if (_targetMonitorBounds.HasValue)
            {
                var bounds = _targetMonitorBounds.Value;

                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.None;
                this.Topmost = _strictLock;
                this.ShowInTaskbar = true;
                this.ResizeMode = ResizeMode.NoResize;

                // 전달받은 모니터의 경계 영역으로 위치와 크기를 강제 적용
                this.Left = bounds.Left;
                this.Top = bounds.Top;
                this.Width = bounds.Width;
                this.Height = bounds.Height;
                
                // WindowState.Maximized로 전환 시 보조 모니터 렌더링 버그(하얀 화면)가 나타날 수 있으므로 Normal 상태를 유지합니다.

                if (_compactTop)
                {
                    this.Title = "📚 기록 사이트 - " + _allowedDomain;
                }
                return;
            }

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
            if (_targetMonitorBounds.HasValue) return; // 모니터 지정일 때는 타이머 사용 안 함
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
            if (_targetMonitorBounds.HasValue) return; // 모니터 지정 전체화면 기록 사이트는 탑모스트 강제 제외

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

        async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userDataFolder = System.IO.Path.Combine(localAppData, "FocusGuard", "WebView2");

                if (!System.IO.Directory.Exists(userDataFolder))
                {
                    System.IO.Directory.CreateDirectory(userDataFolder);
                }

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);
                
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
                        if (!IsDomainAllowed(uri.Host, _allowedDomain))
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

                // 로딩 성공 여부 확인
                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        MessageBox.Show($"페이지 로드 실패: {e.WebErrorStatus}\nURL: {_targetUrl}\n인터넷 연결 상태 혹은 기록 사이트 주소를 확인해주세요.", "페이지 로드 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                // 브라우저 렌더러 프로세스 장애 감지
                webView.CoreWebView2.ProcessFailed += (s, e) =>
                {
                    MessageBox.Show($"WebView2 내부 프로세스 장애 발생: {e.ProcessFailedKind}\n원인: {e.Reason}\nGPU 가속 문제 또는 시스템 메모리 부족일 수 있습니다.", "브라우저 프로세스 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 초기화 실패: {ex.Message}\n스택 트레이스:\n{ex.StackTrace}", "오류");
            }
        }

        private bool IsDomainAllowed(string targetHost, string allowedHost)
        {
            if (string.IsNullOrWhiteSpace(targetHost) || string.IsNullOrWhiteSpace(allowedHost))
                return false;

            targetHost = targetHost.ToLowerInvariant();
            allowedHost = allowedHost.ToLowerInvariant();

            if (targetHost.Contains(allowedHost) || allowedHost.Contains(targetHost))
                return true;

            bool isTargetYoutube = targetHost.Contains("youtube.com") || targetHost.Contains("youtu.be");
            bool isAllowedYoutube = allowedHost.Contains("youtube.com") || allowedHost.Contains("youtu.be");
            if (isTargetYoutube && isAllowedYoutube)
                return true;

            return false;
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
