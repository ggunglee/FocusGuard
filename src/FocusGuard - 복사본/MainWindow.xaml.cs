using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Diagnostics;
using FocusGuard.Data;

namespace FocusGuard
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper _dbHelper;

        public MainWindow()
        {
            InitializeComponent();
            _dbHelper = new DatabaseHelper();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMissionsToUI();
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

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void CloseApp_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }
        private void Dashboard_Click(object sender, RoutedEventArgs e) { new DashboardWindow() { Owner = this }.ShowDialog(); }
        private void Settings_Click(object sender, RoutedEventArgs e) { new SettingsWindow() { Owner = this }.ShowDialog(); }

        private void BtnFindFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true) InputRes.Text = dlg.FileName;
        }

        private void BtnAddMission_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTitle.Text)) { MessageBox.Show("과제 이름을 입력하세요."); return; }
            if (!int.TryParse(InputTime.Text, out int mins) || mins <= 0) { MessageBox.Show("목표 시간을 분 단위로 올바르게 입력하세요."); return; }

            string app = string.IsNullOrWhiteSpace(InputApp.Text) ? "notepad" : InputApp.Text.Trim();
            _dbHelper.AddMission(InputTitle.Text, mins, app, InputRes.Text);

            InputTitle.Text = "";
            InputTime.Text = "";
            InputRes.Text = "";

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
            var btn = sender as Button;
            var mission = btn.DataContext as MissionRecord;
            if (mission == null) return;

            if (!int.TryParse(TxtFocusMin.Text, out int fMin) || !int.TryParse(TxtRestMin.Text, out int rMin))
            {
                MessageBox.Show("뽀모도로 시간을 올바른 숫자로 입력해주세요."); return;
            }

            string resource = mission.resource_path ?? "";
            WebLockWindow webWin = null;

            if (resource.StartsWith("http") || resource.StartsWith("www"))
            {
                webWin = new WebLockWindow(resource);
                webWin.Show();
            }
            else if (!string.IsNullOrWhiteSpace(resource))
            {
                try { Process.Start(new ProcessStartInfo(resource) { UseShellExecute = true }); }
                catch { MessageBox.Show("지정된 파일이나 프로그램을 열 수 없습니다. 경로를 다시 확인해주세요."); }
            }

            StudyWindow studyWin = new StudyWindow(mission.title, mission.duration_minutes * 60, fMin * 60, rMin * 60, mission.target_app, webWin);

            // 🔥 핵심 1: 위젯(StudyWindow)을 웹 잠금창(WebLockWindow)의 '자식'으로 등록!
            // 이렇게 하면 윈도우 OS가 위젯을 무조건 브라우저 위로 들어 올려줍니다.
            if (webWin != null)
            {
                studyWin.Owner = webWin;
            }

            studyWin.Show();
        }
    }
}
