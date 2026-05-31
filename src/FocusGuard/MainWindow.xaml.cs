using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using FocusGuard.Data;
using FocusGuard.Services;

namespace FocusGuard
{
    public partial class MainWindow : Window
    {
        private const string ReadingLogToken = "[READING_LOG]";
        private DatabaseHelper _dbHelper;
        private bool _isSessionRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            _dbHelper = new DatabaseHelper();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            LoadMissionsToUI();
            UpdateSelectedAppsText();

            if (Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText().Trim();
                if (!string.IsNullOrEmpty(clip) && clip.Contains(".") && !clip.Contains(" ") && !clip.Contains("\n"))
                    InputRes.Text = clip;
            }
        }

        private void LoadMissionsToUI()
        {
            ListMissions.ItemsSource = _dbHelper.GetMissions().ToList();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            new DashboardWindow() { Owner = this }.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow() { Owner = this }.ShowDialog();
        }

        private void MemoHistory_Click(object sender, RoutedEventArgs e)
        {
            new MemoHistoryWindow() { Owner = this }.ShowDialog();
        }

        private void BtnPasteUrl_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("클립보드에 붙여넣을 텍스트가 없습니다.", "붙여넣기");
                return;
            }

            string clip = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(clip))
            {
                MessageBox.Show("클립보드 내용이 비어 있습니다.", "붙여넣기");
                return;
            }

            AppendResource(clip);
        }

        private void BtnFindFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                AppendResource(dlg.FileName);
            }
        }

        private void BtnPickApp_Click(object sender, RoutedEventArgs e)
        {
            var wm = new WindowManager();
            string currentProcess = Process.GetCurrentProcess().ProcessName;

            var windows = wm.GetOpenWindows()
                .Where(w => !w.ProcessName.Equals(currentProcess, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.ProcessName)
                .ThenBy(w => w.Title)
                .ToList();

            if (windows.Count == 0)
            {
                MessageBox.Show("현재 선택할 수 있는 열린 창이 없습니다.", "앱 찾기");
                return;
            }

            var picker = new Window
            {
                Title = "감시할 앱 선택",
                Width = 640,
                Height = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var guide = new TextBlock
            {
                Text = "열린 창을 선택하면 해당 앱이 집중 허용 앱으로 추가됩니다.",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(guide, 0);
            root.Children.Add(guide);

            var list = new ListBox
            {
                ItemsSource = windows,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(list, 1);
            root.Children.Add(list);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var clear = new Button
            {
                Content = "선택 초기화",
                Width = 100,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var ok = new Button
            {
                Content = "추가",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var cancel = new Button
            {
                Content = "닫기",
                Width = 90,
                Height = 32
            };

            Action choose = () =>
            {
                var selected = list.SelectedItem as OpenWindowInfo;
                if (selected == null)
                {
                    MessageBox.Show("추가할 창을 선택해주세요.", "앱 찾기");
                    return;
                }

                AppendAppName(selected.ProcessName);
            };

            clear.Click += (s, args) =>
            {
                InputApp.Text = "";
                UpdateSelectedAppsText();
            };

            ok.Click += (s, args) => choose();
            cancel.Click += (s, args) =>
            {
                picker.Close();
            };

            list.MouseDoubleClick += (s, args) => choose();

            buttons.Children.Add(clear);
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(buttons, 2);
            root.Children.Add(buttons);

            picker.Content = root;
            picker.ShowDialog();
        }

        private void AppendAppName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;

            var apps = SplitTokens(InputApp.Text).ToList();

            if (!apps.Any(a => a.Equals(processName, StringComparison.OrdinalIgnoreCase)))
            {
                apps.Add(processName);
            }

            InputApp.Text = string.Join(",", apps);
            UpdateSelectedAppsText();
        }

        private void UpdateSelectedAppsText()
        {
            if (TxtSelectedApps == null || InputApp == null) return;

            var apps = SplitTokens(InputApp.Text);
            TxtSelectedApps.Text = apps.Length == 0 ? "선택 앱 없음" : string.Join(", ", apps);
        }

        private void AppendResource(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) return;

            var items = SplitTokens(InputRes.Text).ToList();

            if (!items.Any(x => x.Equals(resource, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(resource);
            }

            InputRes.Text = string.Join(", ", items);
        }

        private void BtnPresetTime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int addMins))
            {
                int currentMins = 0;
                if (!string.IsNullOrWhiteSpace(InputTime.Text))
                {
                    int.TryParse(InputTime.Text, out currentMins);
                }
                InputTime.Text = (currentMins + addMins).ToString();
            }
        }

        private void BtnPresetTimeClear_Click(object sender, RoutedEventArgs e)
        {
            InputTime.Text = "";
        }

        private void BtnAddMission_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTitle.Text))
            {
                MessageBox.Show("과제 이름을 입력하세요.");
                return;
            }

            if (!int.TryParse(InputTime.Text, out int mins) || mins <= 0)
            {
                MessageBox.Show("목표 시간을 분 단위로 올바르게 입력하세요.");
                return;
            }

            var resources = SplitTokens(InputRes.Text).ToList();

            if (ChkUseReadingLog.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(AppSettings.ReadingLogUrl))
                {
                    MessageBox.Show("환경 설정에서 기록용 사이트 URL을 먼저 저장해주세요.", "기록 사이트 URL 필요");
                    return;
                }

                if (!resources.Any(r => IsReadingLogToken(r)))
                {
                    resources.Add(ReadingLogToken);
                }
            }

            string app = string.Join(",", SplitTokens(InputApp.Text));
            string res = string.Join(", ", resources);

            _dbHelper.AddMission(InputTitle.Text, mins, app, res);

            InputTitle.Text = "";
            InputTime.Text = "";
            InputRes.Text = "";
            InputApp.Text = "";
            ChkUseReadingLog.IsChecked = false;
            UpdateSelectedAppsText();

            LoadMissionsToUI();
        }

        private void BtnDeleteMission_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var mission = btn.DataContext as MissionRecord;

            if (mission != null)
            {
                if (MessageBox.Show($"'{mission.title}' 과제를 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dbHelper.DeleteMission(mission.id);
                    LoadMissionsToUI();
                }
            }
        }

        private void BtnStartMission_Click(object sender, RoutedEventArgs e)
        {
            if (_isSessionRunning) return;

            var btn = sender as Button;
            var mission = btn.DataContext as MissionRecord;
            if (mission == null) return;

            if (!int.TryParse(TxtFocusMin.Text, out int fMin) || !int.TryParse(TxtRestMin.Text, out int rMin))
            {
                MessageBox.Show("뽀모도로 시간을 올바른 숫자로 입력해주세요.");
                return;
            }

            string[] rawResources = SplitTokens(mission.resource_path);
            string[] appTargets = SplitTokens(mission.target_app);

            var readingLogRequested = rawResources.Any(IsReadingLogToken);
            var normalResources = rawResources.Where(r => !IsReadingLogToken(r)).ToList();

            var webResources = normalResources.Where(IsWebResource).ToList();
            var nonWebResources = normalResources.Where(r => !IsWebResource(r)).ToList();

            bool hasRealAppTarget = appTargets.Any(a => !IsBrowserPlaceholder(a));

            bool normalWebStrict =
                webResources.Count == 1 &&
                !hasRealAppTarget &&
                nonWebResources.Count == 0;

            var webWins = new List<WebLockWindow>();

            foreach (string web in webResources)
            {
                var webWin = new WebLockWindow(web, strictLock: normalWebStrict, compactTop: false);
                webWins.Add(webWin);
                webWin.Show();
            }

            if (readingLogRequested)
            {
                if (string.IsNullOrWhiteSpace(AppSettings.ReadingLogUrl))
                {
                    MessageBox.Show("기록용 사이트 URL이 비어 있어 기록 사이트 창은 열지 않습니다.\n환경 설정에서 URL을 저장해주세요.", "기록 사이트 URL 없음");
                }
                else
                {
                    var logWin = new WebLockWindow(AppSettings.ReadingLogUrl, strictLock: false, compactTop: true);
                    webWins.Add(logWin);
                    logWin.Show();
                }
            }

            foreach (string resource in nonWebResources)
            {
                if (string.IsNullOrWhiteSpace(resource)) continue;

                try
                {
                    Process.Start(new ProcessStartInfo(resource) { UseShellExecute = true });
                }
                catch
                {
                    MessageBox.Show($"지정된 파일이나 프로그램을 열 수 없습니다.\n경로: {resource}", "실행 실패");
                }
            }

            string[] effectiveAppTargets = appTargets;

            if (webResources.Count > 0)
            {
                effectiveAppTargets = appTargets.Where(a => !IsBrowserPlaceholder(a)).ToArray();
            }

            string targetAppsForSession = string.Join(",", effectiveAppTargets);

            StudyWindow studyWin = new StudyWindow(
                mission.title,
                mission.duration_minutes * 60,
                fMin * 60,
                rMin * 60,
                targetAppsForSession,
                webWins
            );

            var ownerWeb = webWins.FirstOrDefault(w => w.IsStrictLock && !w.IsCompactTop);
            if (ownerWeb != null)
            {
                studyWin.Owner = ownerWeb;
            }

            _isSessionRunning = true;
            studyWin.Closed += (s, ev) =>
            {
                _isSessionRunning = false;
                this.Show();
            };

            studyWin.Show();
            this.Hide();
        }

        private string[] SplitTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new string[0];

            return text
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        private bool IsReadingLogToken(string value)
        {
            return string.Equals(value?.Trim(), ReadingLogToken, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWebResource(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            string v = value.Trim();
            return v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   v.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   v.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsBrowserPlaceholder(string app)
        {
            if (string.IsNullOrWhiteSpace(app)) return false;

            string lower = app.Trim().ToLowerInvariant();

            return lower == "chrome" ||
                   lower == "msedge" ||
                   lower == "firefox" ||
                   lower == "iexplore";
        }
    }
}
