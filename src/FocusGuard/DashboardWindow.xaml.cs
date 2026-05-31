using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using FocusGuard.Data;

namespace FocusGuard
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            var dbHelper = new DatabaseHelper();
            var todayRecords = dbHelper.GetTodaySessions();
            int tFocus = 0, tDistract = 0, tCount = 0;
            foreach (var r in todayRecords) { tFocus += r.actual_focused_seconds; tDistract += r.distracted_seconds; tCount++; }
            
            TxtTodaySessions.Text = $"{tCount}개";
            TxtTodayFocus.Text = FormatTime(tFocus, false);
            TxtTodayDistract.Text = $"{tDistract}초";
            TxtTodayRatio.Text = CalculateRatio(tFocus, tDistract);

            var weekRecords = dbHelper.GetWeeklySessions();
            int wFocus = 0, wDistract = 0, wCount = 0;
            foreach (var r in weekRecords) { wFocus += r.actual_focused_seconds; wDistract += r.distracted_seconds; wCount++; }
            
            TxtWeekSessions.Text = $"{wCount}개";
            TxtWeekFocus.Text = FormatTime(wFocus, true);
            TxtWeekDistract.Text = FormatTime(wDistract, false);
            TxtWeekRatio.Text = CalculateRatio(wFocus, wDistract);

            var dateStrings = dbHelper.GetStudyDates().ToList();
            int streak = 0;
            DateTime checkDate = DateTime.Now.Date;

            if (dateStrings.Contains(checkDate.ToString("yyyy-MM-dd"))) { streak++; checkDate = checkDate.AddDays(-1); }
            else if (dateStrings.Contains(checkDate.AddDays(-1).ToString("yyyy-MM-dd"))) { checkDate = checkDate.AddDays(-1); }

            if (streak > 0 || dateStrings.Contains(checkDate.ToString("yyyy-MM-dd")))
            {
                while (dateStrings.Contains(checkDate.ToString("yyyy-MM-dd"))) { streak++; checkDate = checkDate.AddDays(-1); }
            }
            TxtStreak.Text = streak > 0 ? $"{streak}일째!" : "오늘부터 1일!";
        }

        // 🔥 CSV 내보내기 핵심 기능
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dbHelper = new DatabaseHelper();
                var allSessions = dbHelper.GetWeeklySessions(); // 최근 기록 로드
                
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "FocusGuard_Records.csv");

                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("과제명,실제 집중 시간(초),딴짓 감지 시간(초),기록 시간");

                foreach (var r in allSessions)
                {
                    csvContent.AppendLine($"\"{r.target_name}\",{r.actual_focused_seconds},{r.distracted_seconds},{r.started_at}");
                }

                // 엑셀에서 한글이 깨지지 않도록 UTF-8 BOM 인코딩 처리하여 저장
                File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
                MessageBox.Show($"바탕화면에 성공적으로 CSV 파일이 저장되었습니다!\n경로: {filePath}", "내보내기 성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 저장 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private string FormatTime(int seconds, bool includeHours)
        {
            if (seconds == 0) return "0분";
            if (includeHours && seconds >= 3600) return $"{seconds / 3600}시간 {(seconds % 3600) / 60}분";
            if (seconds >= 60) return $"{seconds / 60}분 {seconds % 60}초";
            return $"{seconds}초";
        }

        private string CalculateRatio(int focus, int distract)
        {
            int total = focus + distract;
            if (total == 0) return "0%";
            return $"{((double)focus / total * 100):F1}%";
        }

        private void Close_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}
