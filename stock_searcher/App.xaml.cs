using nnns.data;
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
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 打开控制台（用于临时调试）
            if (e.Args.Length > 0 && e.Args[0] == "console") AllocConsole();

            // 如果命令是：-r 路径，则直接开始查库存，查完关闭
            if (e.Args.Length == 2 && e.Args[0] == "-r") NnReader.AutoSearchPath = e.Args[1];// 这里这样写是因为=比==优先级低

        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }
    }
}
