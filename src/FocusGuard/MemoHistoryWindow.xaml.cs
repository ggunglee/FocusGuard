using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusGuard.Data;

namespace FocusGuard
{
    public partial class MemoHistoryWindow : Window
    {
        private readonly DatabaseHelper _dbHelper;
        private List<SessionRecordViewModel> _allSessionViewModels;

        public MemoHistoryWindow()
        {
            InitializeComponent();
            _dbHelper = new DatabaseHelper();
            _allSessionViewModels = new List<SessionRecordViewModel>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSessions();
        }

        private void LoadSessions()
        {
            try
            {
                var sessions = _dbHelper.GetAllSessions();
                _allSessionViewModels = sessions.Select(s => new SessionRecordViewModel { Record = s }).ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"기록을 불러오는 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private void ApplyFilter()
        {
            bool onlyWithMemo = ChkOnlyWithMemo.IsChecked == true;
            
            var filtered = _allSessionViewModels;
            if (onlyWithMemo)
            {
                filtered = _allSessionViewModels.Where(vm => !string.IsNullOrWhiteSpace(vm.Record.memo)).ToList();
            }

            ListSessions.ItemsSource = filtered;

            // 상세 패널 초기화
            PanelMeta.Visibility = Visibility.Collapsed;
            PanelPlaceholder.Visibility = Visibility.Visible;
            TxtDetailMemo.Text = string.Empty;
        }

        private void ListSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedVm = ListSessions.SelectedItem as SessionRecordViewModel;
            if (selectedVm == null)
            {
                PanelMeta.Visibility = Visibility.Collapsed;
                PanelPlaceholder.Visibility = Visibility.Visible;
                TxtDetailMemo.Text = string.Empty;
                BtnCopyMemo.Visibility = Visibility.Collapsed;
                return;
            }

            var record = selectedVm.Record;

            PanelPlaceholder.Visibility = Visibility.Collapsed;
            PanelMeta.Visibility = Visibility.Visible;

            TxtDetailTitle.Text = selectedVm.DisplayTitle;
            TxtDetailTime.Text = $"공부 시작: {selectedVm.DisplayTime}";

            TxtDetailPlanned.Text = $"{record.planned_study_seconds / 60}분";
            if (record.total_elapsed_seconds > 0)
            {
                TxtDetailPlanned.Text += $" (실소요: {record.total_elapsed_seconds / 60}분)";
            }
            
            // 집중 및 딴짓 시간 포맷
            int focusMin = record.actual_focused_seconds / 60;
            int focusSec = record.actual_focused_seconds % 60;
            if (focusMin > 0)
                TxtDetailFocused.Text = $"{focusMin}분 {focusSec}초";
            else
                TxtDetailFocused.Text = $"{focusSec}초";

            int distractMin = record.distracted_seconds / 60;
            int distractSec = record.distracted_seconds % 60;
            if (distractMin > 0)
                TxtDetailDistracted.Text = $"{distractMin}분 {distractSec}초";
            else
                TxtDetailDistracted.Text = $"{distractSec}초";

            if (string.IsNullOrWhiteSpace(record.memo))
            {
                TxtDetailMemo.Text = "이 세션에는 기록된 메모가 없습니다.";
                BtnCopyMemo.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtDetailMemo.Text = record.memo;
                BtnCopyMemo.Visibility = Visibility.Visible;
            }
        }

        private void ChkOnlyWithMemo_Changed(object sender, RoutedEventArgs e)
        {
            if (ListSessions != null) // 컴포넌트 초기화 중 발생 가능성 방지
            {
                ApplyFilter();
            }
        }

        private void BtnCopyMemo_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtDetailMemo.Text) && TxtDetailMemo.Text != "이 세션에는 기록된 메모가 없습니다.")
            {
                try
                {
                    Clipboard.SetText(TxtDetailMemo.Text);
                    MessageBox.Show("메모 내용이 클립보드에 복사되었습니다.", "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}", "오류");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SessionRecordViewModel
    {
        public SessionRecord Record { get; set; }

        public string DisplayTitle
        {
            get
            {
                if (string.IsNullOrEmpty(Record.target_name)) return "미지정 과제";
                return Record.target_name.Trim('[', ']');
            }
        }

        public string DisplayTime
        {
            get
            {
                if (string.IsNullOrEmpty(Record.started_at)) return "";
                try
                {
                    if (DateTime.TryParse(Record.started_at, out DateTime dt))
                    {
                        return dt.ToString("yyyy-MM-dd HH:mm");
                    }
                }
                catch { }
                return Record.started_at;
            }
        }

        public Visibility MemoVisibility
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(Record.memo)) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
