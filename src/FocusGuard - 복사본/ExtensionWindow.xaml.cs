using System.Windows;

namespace FocusGuard
{
    public partial class ExtensionWindow : Window
    {
        public int ExtraMinutes { get; private set; } = 0;

        public ExtensionWindow()
        {
            InitializeComponent();
        }

        private void BtnExtend_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtExtraMinutes.Text, out int mins) && mins > 0)
            {
                ExtraMinutes = mins;
                this.DialogResult = true;
                this.Close();
            }
            else MessageBox.Show("올바른 숫자를 입력하세요.");
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
