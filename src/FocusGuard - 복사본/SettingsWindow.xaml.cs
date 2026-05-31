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
            TxtOpacityValue.Text = $"현재 투명도: {(int)(SliderOpacity.Value * 100)}%";
            SliderOpacity.ValueChanged += (s, e) => { TxtOpacityValue.Text = $"현재 투명도: {(int)(SliderOpacity.Value * 100)}%"; };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.FocusedOpacity = SliderOpacity.Value;
            AppSettings.IsRealRestMode = ChkRealRest.IsChecked == true; // 설정 저장
            MessageBox.Show("설정이 저장되었습니다.", "알림");
            this.Close();
        }
    }
}
