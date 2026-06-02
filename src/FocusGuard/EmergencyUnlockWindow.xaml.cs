using System;
using System.Collections.Generic;
using System.Windows;

namespace FocusGuard
{
    public partial class EmergencyUnlockWindow : Window
    {
                private string[] _quotes = new string[]
        {
            "The secret of getting ahead is getting started.",
            "The man who does not read has no advantage over the man who cannot read.",
            "Courage is resistance to fear, mastery of fear, not absence of fear.",
            "Kindness is the language which the deaf can hear and the blind can see.",
            "Truth is stranger than fiction, but it is because Fiction is obliged to stick to possibilities; Truth isn't.",
            "Whenever you find yourself on the side of the majority, it is time to pause and reflect.",
            "Good friends, good books, and a sleepy conscience: this is the ideal life.",
            "I have never let my schooling interfere with my education.",
            "The fear of death follows from the fear of life.",
            "A person who won't read has no advantage over one who can't read.",
            "Keep away from people who try to belittle your ambitions.",
            "Action speaks louder than words but not nearly as often.",
            "The best way to cheer yourself is to try to cheer someone else up.",
            "Age is an issue of mind over matter. If you don't mind, it doesn't matter.",
            "The lack of money is the root of all evil.",
            "Name the greatest of all inventors. Accident.",
            "To succeed in life, you need two things: ignorance and confidence.",
            "It is better to keep your mouth closed and let people think you are a fool than to open it and remove all doubt.",
            "Get your facts first, then you can distort them as you please.",
            "Clothes make the man. Naked people have little or no influence on society.",
            "The reports of my death are greatly exaggerated.",
            "Part of the secret of success in life is to eat what you like and let the food fight it out inside.",
            "A clear conscience is the sure sign of a bad memory.",
            "Don't part with your illusions. When they are gone you may still exist, but you have ceased to live.",
            "The difference between the almost right word and the right word is really a large matter.",
            "Travel is fatal to prejudice, bigotry, and narrow-mindedness.",
            "The human race has one really effective weapon, and that is laughter.",
            "The worst loneliness is not to be comfortable with yourself.",
            "If you tell the truth, you don't have to remember anything.",
            "Continuous improvement is better than delayed perfection.",
            "It was the best of times, it was the worst of times.",
            "Please, sir, I want some more.",
            "No one is useless in this world who lightens the burdens of another.",
            "Have a heart that never hardens, and a temper that never tires.",
            "There are dark shadows on the earth, but its lights are stronger.",
            "The pain of parting is nothing to the joy of meeting again.",
            "A loving heart is the truest wisdom.",
            "Procrastination is the thief of time.",
            "Never close your lips to those whom you have already opened your heart.",
            "Reflect upon your present blessings, of which every man has many.",
            "The broken heart. You think you will die, but you keep living, day after day after terrible day.",
            "There is nothing in the world so irresistibly contagious as laughter and good humor.",
            "Every traveler has a home of his own, and he learns to appreciate it the more from his wandering.",
            "We forge the chains we wear in life.",
            "Suffering has been stronger than all other teaching.",
            "Take nothing on its looks; take everything on evidence.",
            "I loved her against reason, against promise, against peace, against hope.",
            "There are books of which the backs and covers are by far the best parts.",
            "Minds, like bodies, will often fall into a pimpled, ill-conditioned state from mere excess of comfort.",
            "The civility which money will purchase, is rarely extended to those who have none.",
            "Be yourself; everyone else is already taken.",
            "To live is the rarest thing in the world. Most people exist, that is all.",
            "We are all in the gutter, but some of us are looking at the stars.",
            "Experience is simply the name we give our mistakes.",
            "The truth is rarely pure and never simple.",
            "Nowadays people know the price of everything and the value of nothing.",
            "A thing is not necessarily true because a man dies for it.",
            "The books that the world calls immoral are books that show the world its own shame.",
            "The only way to get rid of temptation is to yield to it.",
            "I can resist everything except temptation.",
            "With freedom, books, flowers, and the moon, who could not be happy?",
            "A cynic is a man who knows the price of everything and the value of nothing.",
            "I have the simplest tastes. I am always satisfied with the best.",
            "The suspense is terrible. I hope it will last.",
            "Some cause happiness wherever they go; others whenever they go.",
            "A man's face is his autobiography. A woman's face is her work of fiction.",
            "Memory is the diary that we all carry about with us.",
            "Life is far too important a thing ever to talk seriously about.",
            "There is only one thing in the world worse than being talked about, and that is not being talked about.",
            "The world is a stage, but the play is badly cast.",
            "To be, or not to be, that is the question.",
            "The rest is silence.",
            "Brevity is the soul of wit.",
            "All the world's a stage, and all the men and women merely players.",
            "The course of true love never did run smooth.",
            "Cowards die many times before their deaths.",
            "Uneasy lies the head that wears a crown.",
            "What's past is prologue.",
            "Parting is such sweet sorrow.",
            "Hell is empty and all the devils are here.",
            "This above all: to thine own self be true.",
            "There is nothing either good or bad, but thinking makes it so.",
            "We know what we are, but know not what we may be.",
            "Some are born great, some achieve greatness, and some have greatness thrust upon them.",
            "Love all, trust a few, do wrong to none.",
            "Our doubts are traitors, and make us lose the good we oft might win.",
            "The fault, dear Brutus, is not in our stars, but in ourselves.",
            "One touch of nature makes the whole world kin.",
            "Sweet are the uses of adversity.",
            "I must be cruel only to be kind.",
            "Words, words, words.",
            "What's done cannot be undone.",
            "The fool doth think he is wise, but the wise man knows himself to be a fool.",
            "When sorrows come, they come not single spies, but in battalions.",
            "Expectation is the root of all heartache.",
            "It is a truth universally acknowledged, that a single man in possession of a good fortune, must be in want of a wife.",
            "There is no charm equal to tenderness of heart.",
            "Vanity and pride are different things.",
            "Selfishness must always be forgiven, you know, because there is no hope of a cure.",
            "I declare after all there is no enjoyment like reading!",
            "Think only of the past as its remembrance gives you pleasure.",
            "A lady's imagination is very rapid; it jumps from admiration to love, from love to matrimony in a moment.",
            "There are as many forms of love as there are moments in time.",
            "Know your own happiness.",
            "I must learn to be content with being happier than I deserve.",
            "To sit in the shade on a fine day and look upon verdure is the most perfect refreshment.",
            "I was quiet, but I was not blind.",
            "There is nothing like staying at home for real comfort.",
            "Run mad as often as you choose, but do not faint.",
            "One half of the world cannot understand the pleasures of the other.",
            "I am no bird; and no net ensnares me.",
            "Reader, I married him.",
            "I would always rather be happy than dignified.",
            "Life appears to me too short to be spent in nursing animosity.",
            "The soul, fortunately, has an interpreter.",
            "Conventionality is not morality.",
            "I care for myself. The more solitary, the more friendless, the more unsustained I am, the more I will respect myself.",
            "Better to be without logic than without feeling.",
            "I ask you to pass through life at my side.",
            "The human heart has hidden treasures.",
            "Whatever our souls are made of, his and mine are the same.",
            "He's more myself than I am.",
            "If all else perished, and he remained, I should still continue to be.",
            "I have not broken your heart — you have broken it.",
            "Treachery and violence are spears pointed at both ends.",
            "Proud people breed sad sorrows for themselves.",
            "I wish I were a girl again, half savage and hardy, and free.",
            "Honest people don't hide their deeds.",
            "Call me Ishmael.",
            "It is better to fail in originality than to succeed in imitation.",
            "I know not all that may be coming, but be it what it will, I'll go to it laughing.",
            "A whale ship was my Yale College and my Harvard.",
            "To produce a mighty book, you must choose a mighty theme.",
            "There is no folly of the beasts of the earth which is not infinitely outdone by the madness of men.",
            "Ignorance is the parent of fear.",
            "Truth uncompromisingly told will always have its ragged edges.",
            "Better sleep with a sober cannibal than a drunken Christian.",
            "Meditation and water are wedded forever.",
            "행복한 가정은 모두 모습이 비슷하고, 불행한 가정은 모두 제각각의 불행을 안고 있다.",
            "새는 알에서 나오려고 투쟁한다. 알은 세계다. 태어나려고 하는 자는 한 세계를 깨뜨리지 않으면 안 된다.",
            "가장 중요한 것은 눈에 보이지 않아."
        };
        private static readonly Random _rand = new Random();
        private static readonly List<int> _recentIndices = new List<int>();
        private string _currentQuote = "";

        public EmergencyUnlockWindow()
        {
            InitializeComponent();

            int index = 0;
            int maxAttempts = 100;
            for (int i = 0; i < maxAttempts; i++)
            {
                index = _rand.Next(_quotes.Length);
                if (!_recentIndices.Contains(index))
                {
                    break;
                }
            }

            _recentIndices.Add(index);
            if (_recentIndices.Count > 30) // 최근 30개 명언까지 중복 회피
            {
                _recentIndices.RemoveAt(0);
            }

            _currentQuote = _quotes[index];
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

        private void InputQuote_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                BtnConfirm_Click(sender, e);
            }
        }
    }
}


