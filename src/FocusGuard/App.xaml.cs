using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using FocusGuard.Data;
using Microsoft.Web.WebView2.Core;

namespace FocusGuard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings.Load();

            if (!EnsureWebView2Runtime())
            {
                MessageBox.Show(
                    "Microsoft Edge WebView2 Runtime 설치가 필요합니다.\n" +
                    "자동 설치에 실패했거나 설치 파일을 찾지 못했습니다.\n\n" +
                    "WebView2 Runtime을 설치한 뒤 다시 실행해주세요.",
                    "WebView2 Runtime 필요",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
                return;
            }

            var dbHelper = new DatabaseHelper();
            dbHelper.InitializeDatabase();
        }

        private bool EnsureWebView2Runtime()
        {
            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();

                if (!string.IsNullOrWhiteSpace(version))
                {
                    return true;
                }
            }
            catch
            {
                // WebView2 Runtime이 없으면 여기로 들어옴
            }

            return TryInstallWebView2Runtime();
        }

        private bool TryInstallWebView2Runtime()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string[] candidateInstallers =
                {
                    Path.Combine(baseDir, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"),
                    Path.Combine(baseDir, "MicrosoftEdgeWebView2Setup.exe")
                };

                string installerPath = null;

                foreach (string path in candidateInstallers)
                {
                    if (File.Exists(path))
                    {
                        installerPath = path;
                        break;
                    }
                }

                if (installerPath == null)
                {
                    return false;
                }

                MessageBox.Show(
                    "이 PC에 Microsoft Edge WebView2 Runtime이 없어 자동 설치를 시작합니다.\n" +
                    "설치가 끝날 때까지 잠시 기다려주세요.",
                    "WebView2 Runtime 설치",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                var psi = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/silent /install",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit();
                }

                try
                {
                    string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                    return !string.IsNullOrWhiteSpace(version);
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
