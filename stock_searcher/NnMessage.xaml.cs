using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            if (message == null)
            {
                message = new NnMessage();
            }
            message.Text = mes;
            message.Show();
            message.Top = SystemParameters.WorkArea.Height * 0.618 - message.Height / 2;
            message.Left = (SystemParameters.PrimaryScreenWidth - message.Width) / 2;
            new Thread(_animation).Start();
        }

        private static void _animation()
        {
            Thread.Sleep(4500);
            message.Dispatcher.BeginInvoke(new Action(() =>
            {
                DoubleAnimation da = new DoubleAnimation();
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                message.BeginAnimation(Window.OpacityProperty, da);
            }));
            Thread.Sleep(500);
            message.Dispatcher.BeginInvoke(new Action(() =>
            {
                message.Close();
            }));
        }

        public string Text { set => _text.Text = value; }

        public NnMessage()
        {
            InitializeComponent();
        }
    }
}
