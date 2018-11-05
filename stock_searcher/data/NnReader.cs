using Microsoft.Office.Interop.Excel;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.OleDb;

namespace nnns.data
{
    // excel workbook获取静态类
    sealed class NnExcelFactory
    {
        private static Application application = null;
        private static readonly object SynObject = new object();

        NnExcelFactory() { }

        public static Workbook getWorkBook(string url = null)
        {
            if (application == null)
            {
                lock (SynObject)
                {
                    if (application == null)
                    {
                        application = new Application();
                        //application.DisplayAlerts = false;
                        application.Visible = false;
                    }
                }
            }
            if (url == null) return application.Workbooks.Add(Type.Missing);// 如果url为空，则建立新表
            return application.Workbooks.Open(url, Type.Missing);
        }

        public static void Quit()
        {
            if (application != null)
            {
                Console.WriteLine("调用释放资源");
                application.Quit();
                Marshal.ReleaseComObject(application);
                application = null;
            }
        }
    }


    class NnExcelReader
    {
        private bool isReadOnly = true;// 文件是否为只读
        private string url;
        private Workbook workbook;

        public NnExcelReader(string url = null)
        {
            this.url = url;
            initReadOnly();
            workbook = NnExcelFactory.getWorkBook(url);
        }

        public bool IsReadOnly { get => isReadOnly; }

        private void initReadOnly()
        {
            try
            {
                File.Open(url, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close();
                isReadOnly = false;
            }
            catch { }
        }

        public Worksheet creatSheet(string name)
        {
            Worksheet sheet = workbook.Worksheets.Add();
            sheet.Name = name;
            return sheet;
        }

        public void save() => workbook.Save();

        public void saveAs(string path) => workbook.SaveAs(path);
        
        public Worksheet this[int index]{ get => workbook.Worksheets[index]; }

        public Worksheet this[string name] { get => workbook.Worksheets[name]; }

        public string Url { get => url; }
    }

    class NnAccessReader
    {
        private string url;// 数据库连接字段
        private OleDbConnection connection;

        public NnAccessReader(string url)
        {
            this.url = url;
            connection = new OleDbConnection(this.url);
            connection.Open();
        }

        public OleDbDataReader ExecuteReader(string sql)
        {
            OleDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteReader();// 注意关闭DataReader
        }

        public int ExecuteNonQuery(string sql)
        {
            OleDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        // 析构函数
        ~NnAccessReader()
        {
            //connection.Close();
        }
    }
}
