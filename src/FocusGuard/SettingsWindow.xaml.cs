using System;
using System.Windows;

namespace FocusGuard
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            SliderOpacity.Value = AppSettings.FocusedOpacity;
            ChkRealRest.IsChecked = AppSettings.IsRealRestMode;
            TxtReadingLogUrl.Text = AppSettings.ReadingLogUrl ?? "";

            TxtOpacityValue.Text = $"현재 투명도: {(int)(SliderOpacity.Value * 100)}%";
            SliderOpacity.ValueChanged += (s, e) =>
            {
                TxtOpacityValue.Text = $"현재 투명도: {(int)(SliderOpacity.Value * 100)}%";
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettings.FocusedOpacity = SliderOpacity.Value;
                AppSettings.IsRealRestMode = ChkRealRest.IsChecked == true;
                AppSettings.ReadingLogUrl = TxtReadingLogUrl.Text.Trim();
                AppSettings.Save();

                MessageBox.Show(
                    $"설정이 저장되었습니다.\n\n저장 위치:\n{AppSettings.SettingsPath}",
                    "알림",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"설정 저장 중 오류가 발생했습니다.\n\n{ex.Message}",
                    "설정 저장 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
