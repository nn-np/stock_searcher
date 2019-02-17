using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace nnns
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
    }

    class Startup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // 打开控制台（用于临时调试）
            if (args.Length > 0 && args[0] == "console") AllocConsole();
                
            App app = new App();

            StockSearcher searcher = new StockSearcher();
            
            System.Uri resourceLocater = new System.Uri("/stock_searcher;component/app.xaml", System.UriKind.Relative);
            System.Windows.Application.LoadComponent(app, resourceLocater);

            app.MainWindow = searcher;

            // 如果命令是：-r 路径，则直接开始查库存，查完关闭
            if (searcher.IsQuit = args.Length == 2 && args[0] == "-r") searcher._start(args[1]);// 这里这样写是因为=比==优先级低
               
            app.Run();
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
