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
    /// Provider=Microsoft.ACE.OLEDB.12.0;Data Source="I:\java file\test_genscript\polypeptideInfo.accdb"
    public partial class StockSearcher : Window
    {
        public StockSearcher()
        {
            InitializeComponent();
            this.Top = SystemParameters.WorkArea.Height * 0.382 - this.Height / 2;
            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
            test();
        }

        private void test()
        {

            NnSearchManager manager = new NnSearchManager(@"C:\Users\nn_np\Desktop\上午新单.xlsx");
            manager.Start();

            Console.WriteLine(ConfigurationManager.ConnectionStrings["nnhistory"].ConnectionString);

            NnConfig config = ConfigurationManager.GetSection("nnconfig") as NnConfig;

            int count = config.TfaFlgs.Count;
            for(int i = 0; i < count; ++i)
            {
                Console.WriteLine(config.TfaFlgs[i].Name+" "+config.TfaFlgs[i].Flg);
            }
        }

        private void bt_colse_click(object sender, RoutedEventArgs e) => Close();

        private void m_down(object sender, MouseButtonEventArgs e) => DragMove();

        // 拖放之后动作
        private void m_drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.None)
                Console.WriteLine(((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());
        }

        private void bt_choose_click(object sender, RoutedEventArgs e)
        {
            _chouse();
        }

        private void _chouse()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "选择表格";
            dialog.Filter = "Excel|*.xls;*.xlsx";
            if(dialog.ShowDialog() == true)
            {
                Console.WriteLine(dialog.FileName);
            }
        }

        private void m_doubleclick(object sender, MouseButtonEventArgs e) => Close();
    }
}
