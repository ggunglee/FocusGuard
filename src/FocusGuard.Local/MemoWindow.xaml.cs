using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FocusGuard.Infrastructure;

namespace FocusGuard
{
    public partial class MemoWindow : Window
    {
        private readonly string _filePath = UserDataPaths.MemoPath;
        private DispatcherTimer _autoSaveTimer;

        public MemoWindow()
        {
            InitializeComponent();
            
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromMilliseconds(500);
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;

            LoadMemo();
        }

        private void LoadMemo()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    TxtMemo.Text = File.ReadAllText(_filePath);
                }
                TxtSaveStatus.Text = "로드 완료";
            }
            catch (Exception)
            {
                TxtSaveStatus.Text = "로드 실패";
            }
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            _autoSaveTimer.Stop();
            SaveMemo();
        }

        private void SaveMemo()
        {
            try
            {
                File.WriteAllText(_filePath, TxtMemo.Text);
                TxtSaveStatus.Text = "자동 저장 완료";
            }
            catch (Exception)
            {
                TxtSaveStatus.Text = "저장 실패";
            }
        }

        private void TxtMemo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_autoSaveTimer == null) return;
            
            TxtSaveStatus.Text = "저장 중...";
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void ChkTopmost_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void ChkTopmost_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
                SaveMemo();
            }
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
                SaveMemo();
            }
            base.OnClosing(e);
        }
    }
}

