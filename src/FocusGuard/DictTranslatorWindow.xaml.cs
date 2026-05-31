using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FocusGuard
{
    public partial class DictTranslatorWindow : Window
    {
        // TODO: 구글 앱스 스크립트 배포 후 얻게 될 Web App URL을 여기에 입력하세요.
        private const string ScriptUrl = "https://script.google.com/macros/s/AKfycbxSP-CoiQLiuId0mup2FOQshDfQrh8dw2HVAN_7cVMYkbRc01Q0xCi78Fk46AN_Et2HMw/exec";
        private bool _logsLoaded = false;

        public DictTranslatorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void BtnSearchDict_Click(object sender, RoutedEventArgs e)
        {
            string query = TxtDictInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            TxtDictResult.Text = "검색 중...";
            BtnSearchDict.IsEnabled = false;

            try
            {
                string result = await SendRequestAsync("dictionary", query);
                TxtDictResult.Text = result;
                _logsLoaded = false; // 기록이 추가되었으므로 갱신 플래그 초기화
            }
            catch (Exception ex)
            {
                TxtDictResult.Text = "오류 발생: " + ex.Message;
            }
            finally
            {
                BtnSearchDict.IsEnabled = true;
            }
        }

        private async void BtnTranslate_Click(object sender, RoutedEventArgs e)
        {
            string query = TxtTransInput.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            TxtTransResult.Text = "번역 중...";
            BtnTranslate.IsEnabled = false;

            try
            {
                string result = await SendRequestAsync("translate", query);
                TxtTransResult.Text = result;
                _logsLoaded = false;
            }
            catch (Exception ex)
            {
                TxtTransResult.Text = "오류 발생: " + ex.Message;
            }
            finally
            {
                BtnTranslate.IsEnabled = true;
            }
        }

        private void TxtDictInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                BtnSearchDict_Click(sender, new RoutedEventArgs());
            }
        }

        private void TxtTransInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Shift+Enter 허용 (줄바꿈), 순수 Enter만 검색
                if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != System.Windows.Input.ModifierKeys.Shift)
                {
                    e.Handled = true;
                    BtnTranslate_Click(sender, new RoutedEventArgs());
                }
            }
        }

        private async Task<string> SendRequestAsync(string action, string text, int? rowIndex = null)
        {
            if (ScriptUrl == "YOUR_GOOGLE_APPS_SCRIPT_WEB_APP_URL")
            {
                return "구글 앱스 스크립트 Web App URL이 설정되지 않았습니다. 소스코드를 확인해주세요.";
            }

            using (HttpClient client = new HttpClient())
            {
                object payload;
                if (action == "delete_log")
                {
                    payload = new { action = action, rowIndex = rowIndex };
                }
                else
                {
                    payload = new { action = action, text = text };
                }

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(ScriptUrl, content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                
                try 
                {
                    var resultJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    if (resultJson.TryGetProperty("result", out JsonElement resultEl))
                    {
                        if (action == "get_logs")
                            return resultEl.GetRawText(); // JSON 배열 원본 텍스트 반환
                            
                        return resultEl.GetString();
                    }
                    if (resultJson.TryGetProperty("error", out JsonElement errorEl))
                    {
                        return "서버 오류: " + errorEl.GetString();
                    }
                }
                catch
                {
                }

                return responseBody;
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && MainTabControl.SelectedIndex == 2)
            {
                if (!_logsLoaded)
                {
                    LoadLogsAsync();
                }
            }
        }

        private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            LoadLogsAsync();
        }

        private async void LoadLogsAsync()
        {
            BtnRefreshLogs.IsEnabled = false;
            BtnRefreshLogs.Content = "불러오는 중...";
            
            try
            {
                string jsonResult = await SendRequestAsync("get_logs", "");
                if (!jsonResult.Contains("구글 앱스 스크립트 Web App URL"))
                {
                    var logs = JsonSerializer.Deserialize<List<DictLogViewModel>>(jsonResult);
                    ListLogs.ItemsSource = logs;
                    _logsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("기록을 불러오는 데 실패했습니다.\n" + ex.Message, "오류");
            }
            finally
            {
                BtnRefreshLogs.IsEnabled = true;
                BtnRefreshLogs.Content = "새로고침";
            }
        }

        private void BtnDeleteLog_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int rowIndex = -1;
                
                // Tag가 JsonElement로 들어올 수 있고 직접 int로 들어올 수 있음
                if (btn.Tag is JsonElement je && je.TryGetInt32(out int r))
                {
                    rowIndex = r;
                }
                else if (btn.Tag is int rId)
                {
                    rowIndex = rId;
                }

                if (rowIndex != -1)
                {
                    DeleteRow(rowIndex);
                }
            }
        }

        private async void DeleteRow(int rowIndex)
        {
            if (MessageBox.Show("이 기록을 정말 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    await SendRequestAsync("delete_log", "", rowIndex);
                    LoadLogsAsync(); // 삭제 후 새로고침
                }
                catch (Exception ex)
                {
                    MessageBox.Show("삭제 중 오류가 발생했습니다: " + ex.Message, "오류");
                }
            }
        }
    }

    public class DictLogViewModel
    {
        public int rowIndex { get; set; }
        public string time { get; set; }
        public string type { get; set; }
        public string input { get; set; }
        public string output { get; set; }
    }
}
