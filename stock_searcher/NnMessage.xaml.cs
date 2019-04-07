using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace nnns
{
    /// <summary>
    /// NnMessage.xaml 的交互逻辑
    /// </summary>
    public partial class NnMessage : Window
    {
        private static NnMessage message;

        public static void Show(string mes)
        {
            Application.Current.Dispatcher.Invoke(() => { _show(mes); });
        }

        private static void _show(string mes)
        {
            if (message != null) return;
            message = new NnMessage();
            message.Text = mes;
            message.Show();
            message.Top = SystemParameters.WorkArea.Height * 0.618 - message.Height / 2;
            message.Left = (SystemParameters.PrimaryScreenWidth - message.Width) / 2;
            Task.Delay(4500).ContinueWith(_ => { _animation(); });
            Console.WriteLine(mes);
        }

        private static void _animation()
        {
            message.Dispatcher.Invoke(() =>
            {
                DoubleAnimation da = new DoubleAnimation();
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                message.BeginAnimation(Window.OpacityProperty, da);
            });
            Task.Delay(500).ContinueWith(_ => { message.Dispatcher.Invoke(() => { message.Close(); message = null; }); });
        }

        public string Text { set => _text.Text = value; }

        public NnMessage()
        {
            InitializeComponent();
        }
    }
}
