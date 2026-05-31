using System;
using System.Windows;

namespace FocusGuard
{
    public partial class EmergencyUnlockWindow : Window
    {
        private string[] _quotes = new string[]
        {
            "행복한 가정은 모두 모습이 비슷하고, 불행한 가정은 모두 제각각의 불행을 안고 있다.",
            "새는 알에서 나오려고 투쟁한다. 알은 세계다. 태어나려고 하는 자는 한 세계를 깨뜨리지 않으면 안 된다.",
            "가장 중요한 것은 눈에 보이지 않아.",
            "고생 끝에 낙이 온다.",
            "천 리 길도 한 걸음부터다.",
            "The secret of getting ahead is getting started.",
            "The man who does not read has no advantage over the man who cannot read.",
            "Courage is resistance to fear, mastery of fear, not absence of fear.",
            "Kindness is the language which the deaf can hear and the blind can see.",
            "It was the best of times, it was the worst of times.",
            "Be yourself; everyone else is already taken.",
            "To be, or not to be, that is the question.",
            "There is no charm equal to tenderness of heart.",
            "I am no bird; and no net ensnares me."
        };
        private string _currentQuote = "";

        public EmergencyUnlockWindow()
        {
            InitializeComponent();
            Random rand = new Random();
            _currentQuote = _quotes[rand.Next(_quotes.Length)];
            TxtQuote.Text = _currentQuote;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (InputQuote.Text.Trim() == _currentQuote)
            {
                this.DialogResult = true; // 성공 시 창을 닫으며 성공 신호 반환
                this.Close();
            }
            else MessageBox.Show("문장이 일치하지 않습니다. 띄어쓰기와 마침표를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
