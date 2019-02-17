using Microsoft.Office.Interop.Excel;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.OleDb;
using System.Reflection;

namespace nnns.data
{
    class NnExcelReader
    {
        private bool isReadOnly = true;// 文件是否为只读
        private string url;
        private Application application;
        private Workbook workbook;

        public NnExcelReader(string url = null)
        {
            this.url = url;
            application = new Application();
            application.DisplayAlerts = false;
            initReadOnly();
            workbook = url == null ? application.Workbooks.Add(Type.Missing)
                : application.Workbooks.Open(url, Type.Missing);
        }

        public bool ToOpen { set => workbook.Application.Visible = value; }

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

        public void Save() => workbook.Save();

        public void SaveAs(string path) => workbook.SaveAs(path, XlFileFormat.xlOpenXMLWorkbook);
        
        public Worksheet this[int index]{ get => workbook.Worksheets[index]; }

        public Worksheet this[string name] { get => workbook.Worksheets[name]; }

        public string Url { get => url; }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processid);
        public void Close()
        {
            if (application == null)
                return;
            Console.WriteLine($"释放: {url}");
            try
            {
                workbook.Close();
                application.Quit();
            }
            catch { }
            int pId;
            GetWindowThreadProcessId(new IntPtr(application.Hwnd), out pId);
            System.Diagnostics.Process.GetProcessById(pId).Kill();
            application = null;
        }

        ~NnExcelReader() => Close();

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
            Console.WriteLine(sql);
            using (OleDbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteReader();
            }
        }

        public int ExecuteNonQuery(string sql)
        {
            Console.WriteLine(sql);
            using (OleDbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
