using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using FocusGuard.Services;

namespace FocusGuard
{
    public partial class WebLockWindow : Window
    {
        private string _targetUrl;
        private string _allowedDomain;
        
        // 정식 해제 절차를 밟았는지 확인하는 깃발
        public bool IsAuthorizedToClose { get; set; } = false;

        public WebLockWindow(string url)
        {
            InitializeComponent();
            _targetUrl = url.StartsWith("http") ? url : "https://" + url;
            _allowedDomain = new Uri(_targetUrl).Host;
            
            InitializeAsync();
            
            // 🔥 창이 켜질 때 키보드 단축키 마비 시작
            KeyboardBlocker.Start();
            
            this.StateChanged += (s, e) => {
                if (this.WindowState == WindowState.Minimized) {
                    this.WindowState = WindowState.Maximized;
                    this.Activate();
                    this.Topmost = true;
                }
            };
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate(_targetUrl);
            webView.CoreWebView2.NewWindowRequested += (s, e) => { e.Handled = true; };
            webView.CoreWebView2.NavigationStarting += (s, e) => {
                Uri uri = new Uri(e.Uri);
                if (!uri.Host.Contains(_allowedDomain)) {
                    e.Cancel = true;
                    MessageBox.Show("집중 모드 중에는 다른 사이트로 이동할 수 없습니다!", "차단됨", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }

        // 🔥 강제 종료(Alt+F4 등) 시도 시 막아버리는 로직
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsAuthorizedToClose)
            {
                e.Cancel = true; // 종료 취소!
            }
            else
            {
                KeyboardBlocker.Stop(); // 정식 종료 시 키보드 복구
            }
        }
    }
}
