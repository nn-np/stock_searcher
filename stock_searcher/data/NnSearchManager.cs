using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using System.Net.Sockets;
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
        private Range m_range;// 表格使用的范围
        private string url;

        private NnConfiguration configuration;// 配置文件读写类

        public NnSearchProgress SearchProgress { set; get; }// 注意每次调用这个的时候设置接口

        private int stimes, scount;

        public NnSearchManager(string url)
        {
            init(url);
        }

        public void Start()
        {
            Thread td = new Thread(_begin);
            td.IsBackground = true;
                td.Start();
        }

        private void _begin()
        {
            if (isContinue)// 如果初始化失败，这个值会为false
                _start();
            _end();
            _close();// 释放资源
        }

        private void _close()
        {
            excelReader.Close();
        }

        private void _start()
        {
            int count = 0;

            m_range = excelReader[1].UsedRange;
            m_range.Columns[_info, Type.Missing].Clear();

            int rows = m_range.Rows.Count;

            for(int i = 2; i <= rows; ++i)
            {
                if (!isContinue) return;// 如果用户取消查找库存，就结束

                SearchProgress.progress((float)i / rows);// 通知搜索进度

                try
                {// 包含在try catch里避免一条订单出错影响其他订单的查找
                    NnPolypeptide newPolypeptide = getPolypeptideFromExcel(i);// 得到excel中数据
                    if (newPolypeptide.IsAvailable)// 如果从excel读取的数据有效，则查找库存，存入数据库
                    {
                        ++scount;
                        _insert(newPolypeptide);// 将数据上传到数据库，这个函数有自己的异常处理，错误不会影响后面的执行
                        if (_search(newPolypeptide, i))// 开始搜索数据并写入excel
                            ++count;
                    }
                }
                catch (Exception e)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"第 {i} 行出错！");
                    Console.ResetColor();
                    Console.WriteLine(e.ToString());
                }
                //Console.WriteLine(m_range.Cells[i, 1].Value.GetType().Name+"\n"+ ((DateTime)m_range.Cells[i, 1].Value).ToShortDateString());
            }
            try
            {
                m_range.Cells[1, _info] = $"库存信息: {count}";
            }
            catch(Exception e) { Console.WriteLine(e.ToString()); }
        }

        // 查找库存
        private bool _search(NnPolypeptide p, int row)
        {
            NnStockInfo info = getStockInfoFromAccess(p);
            if (!info.IsAvailable) return false;// 如果库存有效，写入excel并且设置好单元格颜色

            m_range.Cells[row, _info] = info.ToString();
            switch (info.ColorFlg)
            {
                case NnColorFlg.Modification: m_range.Cells[row, _info].Interior.ColorIndex = 45; break;
                case NnColorFlg.Quality: m_range.Cells[row, _info].Interior.ColorIndex = 50; break;
            }
            return true;
        }

        // 将新单信息保存到数据库中
        private void _insert(NnPolypeptide p)
        {
            try
            {
                accessReader.ExecuteNonQuery($"insert into history values('{DateTime.Now}'," +
                    $"{p.WorkNo},'{p.OrderId}','{p.Sequence}',{p.Purity},{p.Mw}," +
                    $"'{p.Modification}','{p.Comments.Replace("'", "''")}')");
            }
            catch { }
        }

        // 从数据库搜索，得到stockInfo对象，注意，这里传入的参数是新单  order by quality desc
        // history.orderId,history.sequence,history.mw,history.modification,history.comments,history.purity,stock_new.cause,stock_new.quality,stock_new.[_date] 
        private NnStockInfo getStockInfoFromAccess(NnPolypeptide p)
        {
            OleDbDataReader reader = accessReader.ExecuteReader("select * from history,stock_new where history.orderId = stock_new.orderId" +
                    $" and sequence = '{p.Sequence}'");
            NnStockInfo info = new NnStockInfo(p);
            while (reader.Read())
            {
                NnStock stock = getStockFromDataReader(reader);
                info.Add(stock);// 这里只添加，由stockInfo判断是否有效，决定是否添加（所以这里添加了，不一定会真添加到库存信息中）
            }
            reader.Close();
            return info;
        }

        // 从dataReader对象获取stock数据
        private NnStock getStockFromDataReader(OleDbDataReader reader)
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

        // 从excel获取多肽对象
        private NnPolypeptide getPolypeptideFromExcel(int row)
        {
            string oId = m_range.Cells[row, orderId].Text;
            string seq = m_range.Cells[row, sequence].Text;
            NnPolypeptide polypeptide = new NnPolypeptide(oId, seq);
            polypeptide.PurityString = m_range.Cells[row, purity].Text;
            polypeptide.QualityString = m_range.Cells[row, quality].Text;
            polypeptide.MwObj = m_range.Cells[row, mw].Value;
            polypeptide.Modification = m_range.Cells[row, modification].Text;
            polypeptide.WorkNoObj = m_range.Cells[row, workNo].Value;
            polypeptide.Comments = m_range.Cells[row, comments].Text;
            return polypeptide;
        }

        // 搜索结束，善后工作
        private void _end()
        {
            SearchProgress.complete(isContinue);// 如果这个值为false，表示任务没有完成
            if (!isContinue) return;

            // 保存
            try
            {
                if (configuration.getBool("issave", false))
                    excelReader.Save();
            }
            catch { NnMessage.Show("无法保存 请确保文件可写"); }
            foreach (NnSavePath path in NnConfig._nnConfig.SavePaths)
            {
                try
                {
                    excelReader.SaveAs(path.Path + Path.GetFileNameWithoutExtension(url));
                }
                catch { NnMessage.Show($"无法保存到 {path.Path}"); }
            }

            configuration.set("scount", scount);
            configuration.set("stimes", stimes + 1);
            configuration.save();
        }

        private void init(string url)
        {
            this.url = url;// 注意，这里的url在传进来之前确保是excel文件，谁要作死传其他的我不管
            try
            {
                configuration = new NnConfiguration();
                // 初始化excel对应列
                initColumn();
            }
            catch (Exception e)
            {
                NnMessage.Show("配置文件读取错误!");
                isContinue = false;
                Console.WriteLine(e.ToString());
                return;
            }
            if(configuration != null)
            {
                stimes = configuration.getInt("stimes", 0) ?? 0;
                scount = configuration.getInt("scount", 0) ?? 0;
            }
            try
            {
                excelReader = new NnExcelReader(url);
                excelReader.ToOpen = configuration.getBool("toopen", false);
                /*if (excelReader.IsReadOnly)
                {
                    NnMessage.Show("所选表格已被占用，关闭表格后再次尝试！");
                    isContinue = false;
                    return;
                }*/
            }
            catch (Exception e)
            {
                NnMessage.Show("无excel组件或文件错误！");
                isContinue = false;
                Console.WriteLine(e.ToString());
                return;
            }
            try
            {
                accessReader = new NnAccessReader(ConfigurationManager.ConnectionStrings["nnhistory"].ConnectionString);
            }
            catch (Exception e)
            {
                NnMessage.Show("数据库初始化错误！");
                isContinue = false;
                Console.WriteLine(e.ToString());
            }
        }

        public bool Stop() => isContinue = false;

        // 初始化excel对应的列
        private void initColumn()
        {
            NnTitleFlgs flgs = NnConfig._nnConfig.TitleFlgs;
            workNo = flgs["workNo"].Flg ?? workNo;
            catalog = flgs["catalogNo"].Flg ?? catalog;
            group = flgs["group"].Flg ?? group;
            orderId = flgs["orderId"].Flg ?? orderId;
            _info = flgs["info"].Flg ?? _info;
            sequence = flgs["sequence"].Flg ?? sequence;
            quality = flgs["quality"].Flg ?? quality;
            purity = flgs["purity"].Flg ?? purity;
            modification = flgs["modification"].Flg ?? modification;
            mw = flgs["mw"].Flg ?? mw;
            comments = flgs["comments"].Flg ?? comments;
        }

        // ----------下面是excel所对应的列------------
        private int workNo = 1;
        private int catalog = 2;
        private int group = 7;
        private int orderId = 12;
        private int _info = 13;
        private int sequence = 14;
        private int quality = 16;
        private int purity = 17;
        private int modification = 18;
        private int mw = 19;
        private int comments = 24;
    }

    // 工作进度接口
    interface NnSearchProgress
    {
        void progress(double progress);// 工作进度

        void complete(bool isComplete);// 表示工作完成，如果为false，则表示任务取消
    }
}
