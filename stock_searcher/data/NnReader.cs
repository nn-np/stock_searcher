using Microsoft.Office.Interop.Excel;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.OleDb;
using System.Reflection;
using System.Configuration;

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

    class NnReader
    {
        private OleDbConnection mConnection;

        public static string AutoSearchPath;

        public bool IsValid;


        private static NnReader mReader;
        private static readonly object locker = new object();

        private NnReader()
        {
#if (DEBUG)
            string path = ConfigurationManager.ConnectionStrings["nnstock_d"].ConnectionString;// 库存路径
            string key = ConfigurationManager.AppSettings["nnkey"];
#else
            string path = ConfigurationManager.ConnectionStrings["nnstock"].ConnectionString;// 库存路径
            string key = NnConnection.NnDecrypt(ConfigurationManager.AppSettings["nnkey"]);
#endif
            if (path == null || key == null)
            {
                NnMessage.ShowMessage("配置文件错误", true);
                return;
            }
            int index = 12;
            while (index < 21)
            {
                try
                {
                    mConnection = new OleDbConnection($"Provider=Microsoft.ACE.OLEDB.{index.ToString()}.0;Data Source={path}");
                    mConnection.Open();
                    IsValid = true;
                    return;
                }
                catch (Exception e) { ++index; Console.WriteLine(e.ToString()); }
            }
            NnMessage.ShowMessage("数据库错误！", true);
        }

        public static NnReader Instance
        {
            get
            {
                if (mReader == null)
                {
                    lock (locker)
                    {
                        if (mReader == null)
                        {
                            mReader = new NnReader();
                        }
                    }
                }
                return mReader;
            }
        }

        public void Colse()
        {
            try
            {
                if(mConnection!=null&& mConnection.State != System.Data.ConnectionState.Closed)
                {
                    mConnection.Dispose();
                }
            }
            catch { }
        }
        /// <summary>
        /// 写入历史数据信息
        /// </summary>
        internal int InsertHistory(NnPolypeptide od)
        {
            int count = 0;
            try
            {
                using (OleDbCommand cmd = new OleDbCommand($"INSERT INTO history VALUES(@v1,@v2,@v3,@v4,@v5,@v6,@v7,@v8)", mConnection))
                {
                    foreach (var v in od.GetObjects())
                    {
                        cmd.Parameters.AddWithValue("", v);
                    }
                    count = cmd.ExecuteNonQuery();
                }
            }
            catch { }
            return count;
        }
        /// <summary>
        /// 获取库存信息
        /// </summary>
        internal NnStockInfo GetStockInfo(NnPolypeptide p)
        {
            NnStockInfo info = new NnStockInfo(p);
            try
            {
                using(OleDbCommand cmd = new OleDbCommand("SELECT * FROM history,stock_new where history.orderId = stock_new.orderId AND history.sequence=@v1", mConnection))
                {
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NnStock stock = _getStockFromDataReader(reader);
                            info.Add(stock);// 这里只添加，由stockInfo判断是否有效，决定是否添加（所以这里添加了，不一定会真添加到库存信息中）
                        }
                    }
                }
            }
            catch { }
            return info;
        }

        /// <summary>
        /// 从dataReader对象获取stock数据
        /// </summary>
        private NnStock _getStockFromDataReader(OleDbDataReader reader)
        {
            string cause = reader["cause"] as string;
            if (!string.IsNullOrWhiteSpace(cause))
                return null;
            string orderId = reader["history.orderId"] as string;
            string sequence = reader["sequence"] as string;
            NnStock stock = new NnStock(orderId, sequence);
            stock.QualitySum = reader["quality"] as string;
            stock.Mw = (double)reader["mw"];
            stock.Purity = (double)reader["purity"];
            stock.Modification = reader["modification"] as string;
            stock.Comments = reader["comments"] as string;

            object dt = reader["_date"];
            if (dt.GetType() != typeof(DBNull))
                stock.Date = (DateTime)dt;
            object wono = reader["workNo"];
            if (wono.GetType() != typeof(DBNull))
                stock.WorkNo = (int)wono;

            return stock;
        }

        // --------------工具-----------------
        public OleDbDataReader ExecuteReader(string sql)
        {
            Console.WriteLine(sql);
            using (OleDbCommand cmd = mConnection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteReader();
            }
        }

        public int ExecuteNonQuery(string sql)
        {
            Console.WriteLine(sql);
            using (OleDbCommand cmd = mConnection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
