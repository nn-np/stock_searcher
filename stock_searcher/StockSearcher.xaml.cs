using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using nnns.data;

namespace nnns
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StockSearcher : Window
    {
        private int btFlg = 0;// 界面上唯一按钮的状态
        private NnSearchManager manager;// 大佬

        public bool IsQuit { get; set; }// 是否退出

        public StockSearcher()
        {
            InitializeComponent();
            this.Top = SystemParameters.WorkArea.Height * 0.382 - this.Height / 2;
            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
            //test();
        }

        private void test()
        {
            manager = new NnSearchManager(@"C:\Users\nn_np\Desktop\上午新单.xlsx");
            manager.SearchProgress = new Progress(this);
            manager.Start();

            Console.WriteLine(ConfigurationManager.ConnectionStrings["nnhistory"].ConnectionString);

            NnConfig config = ConfigurationManager.GetSection("nnconfig") as NnConfig;

            int count = config.TfaFlgs.Count;
            for(int i = 0; i < count; ++i)
            {
                Console.WriteLine(config.TfaFlgs[i].Name+" "+config.TfaFlgs[i].Flg);
            }
        }

        public void _start(string url)
        {
            toStart();
            this.manager = new NnSearchManager(url);
            manager.SearchProgress = new Progress(this);
            manager.Start();
        }

        // 开始
        public void toStart()
        {
            btFlg = 1;
            bt_choose.Content = " 取 消 ";
        }

        // 停止
        public void toStop()
        {
            btFlg = 0;
            bt_choose.Content = "选择文件";
        }

        private void _stop()
        {
            toStop();
            manager.Stop();
        }

        private void bt_colse_click(object sender, RoutedEventArgs e) => Close();

        private void m_down(object sender, MouseButtonEventArgs e) => DragMove();

        // 拖放之后动作
        private void m_drop(object sender, DragEventArgs e)
        {
            string url = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            string str = System.IO.Path.GetExtension(url);
            if (str != ".xlsx" && str != ".xls")
            {
                NnMessage.Show("无效文件");
                return;
            }
            _start(url);
            Console.WriteLine(url);
        }

        private void bt_choose_click(object sender, RoutedEventArgs e)
        {
            if (btFlg == 0)
                _chouse();
            else if (btFlg == 1)
                _stop();
        }

        private void _chouse()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "选择表格";
            dialog.Filter = "Excel|*.xls;*.xlsx";
            if(dialog.ShowDialog() == true)
            {
                _start(dialog.FileName);
                Console.WriteLine(dialog.FileName);
            }
        }

        private void m_doubleclick(object sender, MouseButtonEventArgs e) => Close();


        class Progress : NnSearchProgress
        {
            private StockSearcher stockSearcher;

            public Progress(StockSearcher sc) => this.stockSearcher = sc;

            public void complete(bool isComplete)
            {
                stockSearcher.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (isComplete)
                    {
                        stockSearcher.m_tblock.Text = "数据已保存 选择或拖动文件到此处继续";
                        NnMessage.Show("搜索已完成");
                        Console.WriteLine("Complete");
                    }
                    else
                    {
                        stockSearcher.m_tblock.Text = "选择或拖动文件到此处继续";
                        Console.WriteLine("Cancle");
                    }
                    stockSearcher.progressbar.Value = 0;
                    stockSearcher.toStop();
                    if (stockSearcher.IsQuit)
                        stockSearcher.Close();
                }));
            }

            public void progress(double progress)
            {
                stockSearcher.Dispatcher.BeginInvoke(new Action(() =>
                {
                    stockSearcher.progressbar.Value = progress * 100;
                    stockSearcher.m_tblock.Text = $"正在搜索... {progress.ToString("p")}";
                }));
                Console.WriteLine(progress.ToString("p"));
            }
        }
    }
}
