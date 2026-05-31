using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Media;
using FocusGuard.Services;
using FocusGuard.Data;

namespace FocusGuard
{
    public partial class StudyWindow : Window
    {
        private DispatcherTimer _timer;
        private WindowManager _windowManager;
        private DatabaseHelper _dbHelper;

        private string _taskName;
        private string _targetProcess; // 쉼표로 나열된 멀티 앱 주소
        private int _targetTotalSeconds, _elapsedTotalSeconds;
        private int _focusDuration, _restDuration, _currentPhaseSeconds;
        private int _focusedScore = 0, _distractedScore = 0;
        private bool _isResting = false;

        private WebLockWindow _linkedWebWindow;
        private RestOverlayWindow _restOverlay; // 찐휴식 전용 화면
        public bool IsAuthorizedToClose { get; set; } = false;

        public StudyWindow(string taskName, int totalSec, int focusSec, int restSec, string targetApp, WebLockWindow webWin = null)
        {
            InitializeComponent();
            _windowManager = new WindowManager();
            _dbHelper = new DatabaseHelper();

            _taskName = taskName;
            _targetTotalSeconds = totalSec;
            _focusDuration = focusSec;
            _restDuration = restSec;
            _targetProcess = string.IsNullOrWhiteSpace(targetApp) ? "notepad" : targetApp;
            _linkedWebWindow = webWin;

            _currentPhaseSeconds = Math.Min(_focusDuration, _targetTotalSeconds);
            TxtStatus.Text = _linkedWebWindow != null ? "● 웹 집중 중" : "복합 앱 감시 중...";

            ProgressTimer.Maximum = _currentPhaseSeconds;
            ProgressTimer.Value = _currentPhaseSeconds;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left + 20;
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void WidgetBorder_MouseEnter(object sender, MouseEventArgs e) { this.Opacity = 1.0; }
        private void WidgetBorder_MouseLeave(object sender, MouseEventArgs e) { if (_timer.IsEnabled && !_isResting) this.Opacity = AppSettings.FocusedOpacity; }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.Topmost = true;
            _elapsedTotalSeconds++;
            _currentPhaseSeconds--;

            // 1. 전체 시간 만료
            if (_elapsedTotalSeconds >= _targetTotalSeconds)
            {
                _timer.Stop();
                this.Opacity = 1.0;
                if (_restOverlay != null) { _restOverlay.Close(); _restOverlay = null; }

                ExtensionWindow extWin = new ExtensionWindow();
                if (extWin.ShowDialog() == true)
                {
                    _targetTotalSeconds += extWin.ExtraMinutes * 60;
                    int remainingTotal = _targetTotalSeconds - _elapsedTotalSeconds;
                    int nextPhaseTime = _isResting ? _restDuration : _focusDuration;
                    _currentPhaseSeconds = Math.Min(nextPhaseTime, remainingTotal);
                    ProgressTimer.Maximum = _currentPhaseSeconds;
                    _timer.Start();
                    return;
                }
                else { EndSession(false); return; }
            }

            // 2. 집중 <-> 휴식 페이즈 강제 전환 루프
            if (_currentPhaseSeconds <= 0)
            {
                _isResting = !_isResting;
                int remainingTotal = _targetTotalSeconds - _elapsedTotalSeconds;
                int nextPhaseTime = _isResting ? _restDuration : _focusDuration;
                _currentPhaseSeconds = Math.Min(nextPhaseTime, remainingTotal);
                ProgressTimer.Maximum = _currentPhaseSeconds;

                if (_isResting)
                {
                    // --- ☕ 휴식 시작 페이즈 ---
                    this.Opacity = 1.0;
                    TxtStatus.Text = "☕ 휴식 중";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    ProgressTimer.Foreground = System.Windows.Media.Brushes.Orange;
                    SystemSounds.Asterisk.Play();

                    if (_linkedWebWindow != null) _linkedWebWindow.Visibility = Visibility.Hidden; // 웹뷰 잠금 임시 해제
                    
                    // 🔒 설정에서 찐휴식 모드를 켰다면 풀스크린 락 스크린 실행
                    if (AppSettings.IsRealRestMode)
                    {
                        _restOverlay = new RestOverlayWindow();
                        _restOverlay.Show();
                    }
                }
                else
                {
                    // --- 🎯 집중 강제 복귀 페이즈 (아무런 팝업창 없이 자동으로 돌아감) ---
                    if (_restOverlay != null) { _restOverlay.Close(); _restOverlay = null; } // 찐휴식 오버레이 파괴

                    TxtStatus.Text = "● 집중 중";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    ProgressTimer.Foreground = System.Windows.Media.Brushes.DodgerBlue;
                    SystemSounds.Exclamation.Play();

                    // 웹뷰 자동 대면 소환
                    if (_linkedWebWindow != null)
                    {
                        _linkedWebWindow.Visibility = Visibility.Visible;
                        _linkedWebWindow.WindowState = WindowState.Maximized;
                        _linkedWebWindow.Activate();
                    }
                    else
                    {
                        // 멀티 프로그램 자동 순차적 강제 소환
                        string[] apps = _targetProcess.Split(',');
                        foreach (string app in apps)
                        {
                            _windowManager.BringTargetToForeground(app.Trim());
                        }
                    }
                }
            }

            TxtTimer.Text = $"{_currentPhaseSeconds / 60:D2}:{_currentPhaseSeconds % 60:D2}";
            ProgressTimer.Value = _currentPhaseSeconds;

            // 3. 멀티 앱 교차 감시 시스템
            if (!_isResting)
            {
                if (_linkedWebWindow != null)
                {
                    _focusedScore++;
                    this.Opacity = this.IsMouseOver ? 1.0 : AppSettings.FocusedOpacity;
                    if (_linkedWebWindow.WindowState == WindowState.Minimized) _linkedWebWindow.WindowState = WindowState.Maximized;
                }
                else
                {
                    // 🔥 1번 기능: 쉼표로 나열된 프로그램 중 하나라도 켜져 있는지 검사
                    string[] apps = _targetProcess.Split(',');
                    bool isAnyAppActive = false;

                    foreach (string app in apps)
                    {
                        if (_windowManager.IsTargetWindowActive(app.Trim()))
                        {
                            isAnyAppActive = true;
                            break;
                        }
                    }

                    if (isAnyAppActive)
                    {
                        _focusedScore++;
                        this.Opacity = this.IsMouseOver ? 1.0 : AppSettings.FocusedOpacity;
                        TxtStatus.Text = "● 집중 중";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        _distractedScore++;
                        this.Opacity = 1.0;
                        TxtStatus.Text = "⚠️ 딴짓 감지됨!";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        
                        // 경고 시 첫 번째 앱을 대표로 강제 견인
                        _windowManager.BringTargetToForeground(apps[0].Trim());
                        SystemSounds.Hand.Play();
                    }
                }
            }
        }

        private void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Opacity = 1.0;
            EmergencyUnlockWindow emWin = new EmergencyUnlockWindow();
            if (emWin.ShowDialog() == true) EndSession(true);
            else
            {
                this.Opacity = _isResting ? 1.0 : AppSettings.FocusedOpacity;
                _timer.Start();
            }
        }

        private void EndSession(bool isEmergency)
        {
            _timer.Stop();
            if (_restOverlay != null) { _restOverlay.Close(); _restOverlay = null; }

            _dbHelper.SaveSession($"[{_taskName}]", _targetTotalSeconds, _focusedScore, _distractedScore);
            string msg = isEmergency ? "비상 해제되었습니다." : "과제 달성 완료! 수고하셨습니다.";
            MessageBox.Show($"{msg}\n(집중: {_focusedScore}초 / 딴짓: {_distractedScore}초)\n대시보드에 저장되었습니다.", "알림");

            if (_linkedWebWindow != null) { _linkedWebWindow.IsAuthorizedToClose = true; _linkedWebWindow.Close(); }
            IsAuthorizedToClose = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { if (!IsAuthorizedToClose) e.Cancel = true; }
    }
}
