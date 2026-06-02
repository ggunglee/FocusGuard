using System;
using System.Collections.Generic;
using System.Linq;
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
        private MemoWindow _memoWindow;
        private DictTranslatorWindow _dictWindow;
        private DateTime _sessionStartTime;

        private string _taskName;
        private List<string> _targetApps;
        private List<WebLockWindow> _linkedWebWindows;

        private int _targetTotalSeconds, _elapsedTotalSeconds;
        private int _focusDuration, _restDuration, _currentPhaseSeconds;
        private int _focusedScore = 0, _distractedScore = 0;
        private bool _isResting = false;

        private RestOverlayWindow _restOverlay;
        public bool IsAuthorizedToClose { get; set; } = false;

        public StudyWindow(string taskName, int totalSec, int focusSec, int restSec, string targetApp, WebLockWindow webWin = null)
            : this(taskName, totalSec, focusSec, restSec, targetApp, webWin == null ? null : new List<WebLockWindow> { webWin })
        {
        }

        public StudyWindow(string taskName, int totalSec, int focusSec, int restSec, string targetApp, IEnumerable<WebLockWindow> webWins)
        {
            InitializeComponent();

            _windowManager = new WindowManager();
            _dbHelper = new DatabaseHelper();
            _sessionStartTime = DateTime.Now;

            _taskName = taskName;
            _targetTotalSeconds = totalSec;
            _focusDuration = focusSec;
            _restDuration = restSec;

            _linkedWebWindows = webWins == null
                ? new List<WebLockWindow>()
                : webWins.Where(w => w != null).ToList();

            _targetApps = SplitAppTargets(targetApp);

            if (_targetApps.Count == 0 && _linkedWebWindows.Count == 0)
            {
                _targetApps.Add("notepad");
            }

            _currentPhaseSeconds = Math.Min(_focusDuration, _targetTotalSeconds);

            if (_linkedWebWindows.Count > 0 && _targetApps.Count > 0)
                TxtStatus.Text = "● 멀티 웹/앱 감시 중";
            else if (_linkedWebWindows.Count > 0)
                TxtStatus.Text = "● 웹 집중 중";
            else
                TxtStatus.Text = "복합 앱 감시 중...";

            ProgressTimer.Maximum = _currentPhaseSeconds;
            ProgressTimer.Value = _currentPhaseSeconds;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 공부 세션 시작 시 키보드 차단/윈도우 키 잠금 활성화
            FocusGuard.Services.KeyboardBlocker.Start();
        }

        private List<string> SplitAppTargets(string targetApp)
        {
            if (string.IsNullOrWhiteSpace(targetApp)) return new List<string>();

            return targetApp
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left + 20;
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void WidgetBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1.0;
        }

        private void WidgetBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_timer.IsEnabled && !_isResting) this.Opacity = AppSettings.FocusedOpacity;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TxtCurrentTime.Text = DateTime.Now.ToString("HH:mm");
            
            this.Topmost = true;
            _elapsedTotalSeconds++;
            _currentPhaseSeconds--;

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
                else
                {
                    EndSession(false);
                    return;
                }
            }

            if (_currentPhaseSeconds <= 0)
            {
                _isResting = !_isResting;
                int remainingTotal = _targetTotalSeconds - _elapsedTotalSeconds;
                int nextPhaseTime = _isResting ? _restDuration : _focusDuration;
                _currentPhaseSeconds = Math.Min(nextPhaseTime, remainingTotal);
                ProgressTimer.Maximum = _currentPhaseSeconds;

                if (_isResting)
                {
                    this.Opacity = 1.0;
                    TxtStatus.Text = "☕ 휴식 중";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    ProgressTimer.Foreground = System.Windows.Media.Brushes.Orange;
                    SystemSounds.Asterisk.Play();

                    HideWebWindowsForRest();

                    if (AppSettings.IsRealRestMode)
                    {
                        _restOverlay = new RestOverlayWindow();
                        _restOverlay.Show();
                    }
                }
                else
                {
                    if (_restOverlay != null) { _restOverlay.Close(); _restOverlay = null; }

                    TxtStatus.Text = "● 집중 중";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    ProgressTimer.Foreground = System.Windows.Media.Brushes.DodgerBlue;
                    SystemSounds.Exclamation.Play();

                    ShowWebWindowsForFocus();

                    // 웹뷰 창 유무와 관계없이, 등록된 허용 앱이 있다면 모두 전면으로 소환합니다.
                    if (_targetApps.Count > 0)
                    {
                        foreach (string app in _targetApps)
                        {
                            _windowManager.BringTargetToForeground(app);
                        }
                    }
                }
            }

            TxtTimer.Text = $"{_currentPhaseSeconds / 60:D2}:{_currentPhaseSeconds % 60:D2}";
            ProgressTimer.Value = _currentPhaseSeconds;

            if (!_isResting)
            {
                bool isAllowedActive = IsAnyAllowedTargetActive();

                if (isAllowedActive)
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
                }
            }
        }

        private bool IsAnyAllowedTargetActive()
        {
            if (this.IsActive || this.IsKeyboardFocusWithin) return true;
            if (_memoWindow != null && _memoWindow.IsVisible && (_memoWindow.IsActive || _memoWindow.IsKeyboardFocusWithin)) return true;

            foreach (var webWin in _linkedWebWindows)
            {
                if (webWin == null) continue;


                try
                {
                    if (webWin.IsVisible && (webWin.IsActive || webWin.IsKeyboardFocusWithin))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            foreach (string app in _targetApps)
            {
                if (_windowManager.IsTargetWindowActive(app))
                {
                    return true;
                }
            }

            return false;
        }

        private void HideWebWindowsForRest()
        {
            foreach (var webWin in _linkedWebWindows)
            {
                try
                {
                    if (webWin != null) webWin.Visibility = Visibility.Hidden;
                }
                catch { }
            }
        }

        private void ShowWebWindowsForFocus()
        {
            bool activatedStrictWindow = false;

            foreach (var webWin in _linkedWebWindows)
            {
                try
                {
                    if (webWin == null) continue;

                    webWin.Visibility = Visibility.Visible;

                    if (webWin.WindowState == WindowState.Minimized)
                    {
                        webWin.WindowState = webWin.IsStrictLock ? WindowState.Maximized : WindowState.Normal;
                    }

                    if (webWin.IsStrictLock && !webWin.IsCompactTop && !activatedStrictWindow)
                    {
                        webWin.WindowState = WindowState.Maximized;
                        webWin.Activate();
                        activatedStrictWindow = true;
                    }
                }
                catch { }
            }
        }



        private void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Opacity = 1.0;

            EmergencyUnlockWindow emWin = new EmergencyUnlockWindow();
            if (emWin.ShowDialog() == true)
            {
                EndSession(true);
            }
            else
            {
                this.Opacity = _isResting ? 1.0 : AppSettings.FocusedOpacity;
                _timer.Start();
            }
        }

        private void EndSession(bool isEmergency)
        {
            _timer.Stop();
            FocusGuard.Services.KeyboardBlocker.Stop(); // 키보드 잠금 해제

            string memoText = "";
            if (_memoWindow != null)
            {
                try
                {
                    memoText = _memoWindow.TxtMemo.Text;
                }
                catch { }
            }
            else
            {
                try
                {
                    string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memo.txt");
                    if (System.IO.File.Exists(filePath))
                    {
                        memoText = System.IO.File.ReadAllText(filePath);
                    }
                }
                catch { }
            }

            if (_memoWindow != null)
            {
                try
                {
                    _memoWindow.Close();
                }
                catch { }
                _memoWindow = null;
            }

            if (_restOverlay != null) { _restOverlay.Close(); _restOverlay = null; }

            int totalElapsedSeconds = (int)(DateTime.Now - _sessionStartTime).TotalSeconds;

            _dbHelper.SaveSession($"[{_taskName}]", _targetTotalSeconds, _focusedScore, _distractedScore, totalElapsedSeconds, memoText);

            string msg = isEmergency ? "비상 해제되었습니다." : "과제 달성 완료! 수고하셨습니다.";
            MessageBox.Show($"{msg}\n(집중: {_focusedScore}초 / 딴짓: {_distractedScore}초)\n대시보드에 저장되었습니다.", "알림");

            foreach (var webWin in _linkedWebWindows)
            {
                try
                {
                    if (webWin == null) continue;

                    webWin.IsAuthorizedToClose = true;
                    webWin.Close();
                }
                catch { }
            }

            IsAuthorizedToClose = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsAuthorizedToClose)
            {
                e.Cancel = true;
            }
            else
            {
                FocusGuard.Services.KeyboardBlocker.Stop(); // 안전하게 재차 훅 해제
            }
        }

        private void BtnMemo_Click(object sender, RoutedEventArgs e)
        {
            if (_memoWindow == null)
            {
                _memoWindow = new MemoWindow();
                _memoWindow.Closed += (s, ev) => 
                { 
                    _memoWindow = null;
                    if (!_isResting) _timer.Start(); // 휴식 중이 아닐 때만 재개
                };
            }

            if (!_memoWindow.IsVisible)
            {
                _timer.Stop(); // 메모장 여는 동안 타이머 정지
                _memoWindow.Show();
            }
            
            _memoWindow.Activate();
        }

        private void BtnDictionary_Click(object sender, RoutedEventArgs e)
        {
            if (_dictWindow == null)
            {
                _dictWindow = new DictTranslatorWindow();
                _dictWindow.Closed += (s, ev) => 
                { 
                    _dictWindow = null; 
                    if (!_isResting) _timer.Start(); // 휴식 중이 아닐 때만 재개
                };
            }

            if (!_dictWindow.IsVisible)
            {
                _timer.Stop(); // 사전 여는 동안 타이머 정지
                _dictWindow.Show();
            }
            
            _dictWindow.Activate();
        }
    }
}
