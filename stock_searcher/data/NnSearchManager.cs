using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;
/**
 * 库存搜索的执行类
 * Excel格式：DateTime String Double
 */
namespace nnns.data
{
    class NnSearchManager
    {
        private bool isContinue = true;// 线程是否需要继续运行
        private NnExcelReader excelReader;// excel读写
        private NnAccessReader accessReader;// 数据库读写

        private NnConfiguration configuration;// 配置文件读写类

        public SearchProgress SearchProgress { set => SearchProgress = value; }// 注意每次调用这个的时候设置接口

        public NnSearchManager(string url)
        {
            init(url);
        }

        public void Start()
        {
            Thread td = new Thread(_start);
            td.IsBackground = true;
            td.Start();
        }

        private void _start()
        {
            Worksheet ws = excelReader[1];
            int size = ws.UsedRange.Rows.Count;
            int i = 0;

            Console.WriteLine(ws.UsedRange.Rows.Count);
            foreach(Range row in ws.UsedRange.Rows)
            {
                if (!isContinue) return;// 如果用户取消查找库存，就结束
                ++i;
                
            }
            string str = ws.Cells[692, 1].Text;
            Console.WriteLine(i);
        }

        private void init(string url)
        {
            configuration = new NnConfiguration();

            excelReader = new NnExcelReader(url);
            if (excelReader.IsReadOnly)
            {
                NnMessage.Show("所选表格已被占用，关闭表格后再次尝试！");
                isContinue = false;
                return;
            }
            accessReader = new NnAccessReader(ConfigurationManager.ConnectionStrings["nnhistory"].ConnectionString);
        }

        public bool Stop() => isContinue = false;

        ~NnSearchManager()
        {
            NnExcelFactory.Quit();// TODO 这个再测试一下，是在程序结束的时候调用还是线程结束的时候嗲调用，多次开启和结束excel实例会不会有进程残留
            // 注意每次搜索的时候只有一个搜索线程工作，不然导致excel app出现问题，比如线程1正在使用，线程2将excel app结束了
            // 以后如果需要，可以使用多线程优化，
        }
    }

    interface SearchProgress
    {
        void progress(float progress);// 工作进度

        void complete();// 表示工作完成
    }
}
