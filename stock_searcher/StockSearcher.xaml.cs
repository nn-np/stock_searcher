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
            test();
        }

        private void test()
        {
            NnConfig config = ConfigurationManager.GetSection("nnconfig") as NnConfig;

            int count = config.TfaFlgs.Count;
            for(int i = 0; i < count; ++i)
            {
                Console.WriteLine(config.TfaFlgs[i].Name+" "+config.TfaFlgs[i].Flg);
            }
        }

        private void bt_colse(object sender, RoutedEventArgs e) => Close();

        private void m_down(object sender, MouseButtonEventArgs e) => DragMove();

        // 拖放之后动作
        private void m_drop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.None)
                Console.WriteLine(((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());
        }

        private void bt_chouse(object sender, RoutedEventArgs e)
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
    }
}
