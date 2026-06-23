using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using FocusGuard.Data;
using FocusGuard.Services;

namespace FocusGuard;

public partial class DictTranslatorWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly GoogleTranslationService _translationService;
    private readonly DictionaryHistoryRepository _historyRepository;
    private bool _logsLoaded;

    public DictTranslatorWindow()
    {
        InitializeComponent();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _translationService = new GoogleTranslationService(_httpClient);
        _historyRepository = new DictionaryHistoryRepository();
        _historyRepository.Initialize();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private async void BtnSearchDict_Click(object sender, RoutedEventArgs e)
    {
        await RunTranslationAsync(
            TxtDictInput.Text,
            dictionaryMode: true,
            "사전",
            TxtDictResult,
            BtnSearchDict,
            "검색 중...");
    }

    private async void BtnTranslate_Click(object sender, RoutedEventArgs e)
    {
        await RunTranslationAsync(
            TxtTransInput.Text,
            dictionaryMode: false,
            "번역",
            TxtTransResult,
            BtnTranslate,
            "번역 중...");
    }

    private async Task RunTranslationAsync(
        string rawQuery,
        bool dictionaryMode,
        string historyType,
        TextBox resultBox,
        Button actionButton,
        string progressText)
    {
        string query = rawQuery.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        resultBox.Text = progressText;
        actionButton.IsEnabled = false;

        try
        {
            TranslationResult result = await _translationService.TranslateAsync(query, dictionaryMode);
            resultBox.Text = result.DisplayText;
            _historyRepository.Add(historyType, query, result.DisplayText);
            _logsLoaded = false;
        }
        catch (TranslationServiceException ex)
        {
            resultBox.Text = $"번역 오류: {ex.Message}";
        }
        catch (Exception ex)
        {
            resultBox.Text = $"로컬 저장 오류: {ex.Message}";
        }
        finally
        {
            actionButton.IsEnabled = true;
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
        if (e.Key == System.Windows.Input.Key.Enter &&
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            BtnTranslate_Click(sender, new RoutedEventArgs());
        }
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl && MainTabControl.SelectedIndex == 2 && !_logsLoaded)
        {
            LoadLogs();
        }
    }

    private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void LoadLogs()
    {
        BtnRefreshLogs.IsEnabled = false;
        BtnRefreshLogs.Content = "불러오는 중...";

        try
        {
            ListLogs.ItemsSource = _historyRepository.GetAll();
            _logsLoaded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"기록을 불러오는 데 실패했습니다.\n{ex.Message}", "오류");
        }
        finally
        {
            BtnRefreshLogs.IsEnabled = true;
            BtnRefreshLogs.Content = "새로고침";
        }
    }

    private void BtnDeleteLog_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int rowIndex })
        {
            return;
        }

        if (MessageBox.Show(
                "이 기록을 정말 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _historyRepository.Delete(rowIndex);
            LoadLogs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"삭제 중 오류가 발생했습니다: {ex.Message}", "오류");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _httpClient.Dispose();
        base.OnClosed(e);
    }
}
