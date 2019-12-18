using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/**
 * 多肽类
 * crude为 -2，desalt为 -1
 * nnns
 */
namespace nnns.data
{
    class NnPolypeptide
    {
        private long workNo;// worknumber
        private double purity;// 纯度
        private double mw;// 分子量
        private double quality;// 质量（可能是需要的量，也可能时库存量）
        private string modification;// 修饰
        private string comments;// 备注
        private int tfaflg;// 转盐信息

        public NnPolypeptide(string orderId, string sequence)
        {
            this.orderId = (orderId ?? "").Contains('-') ? orderId : "";
            this.sequence = Regex.Replace(sequence ?? "", @"\s", "").ToUpper();
        }
        private string orderId;// orderId
        /// <summary>
        /// OrderId
        /// </summary>
        public string OrderId { get => orderId ?? ""; set => orderId = value; }
        private string sequence;// 序列
        /// <summary>
        /// Sequence
        /// </summary>
        public string Sequence { get => sequence ?? ""; set => sequence = value; }
        public long WorkNo { get => workNo; set => workNo = value > 9999 ? value : -1; }
        public object WorkNoObj { set => WorkNo = (long)getNum(value); }// 如果从excel表中读取的值类型不确定的话，建议使用这个属性，避免报错闪退
        public double Mw { get => mw; set => mw = value; }
        public string MwString { get => mw.ToString(); set => mw = getMaxValue(value); }
        public object MwObj { set => mw = getNum(value); }
        public double Quality { get => quality; set => quality = value; }
        public string QualityString { get => $"{quality}mg"; set => quality = getMaxValue(value); }// 这个是得到字符串中的最大值作为质量
        public string QualitySum { get => $"{quality}mg"; set => quality = getSumValue(value); }// 这个是得到字符串中数字的和作为质量
        public string Modification { get => modification; set => modification = Regex.Replace(value ?? "", @"\s", ""); }// TODO  提取修饰信息？，这里要干什么忘记了，想起来了，这里匹配是要忽略大小写
        public string Comments { get => comments; set => comments = intiComments(value); }

        public int Tfaflg { get => tfaflg; }
        public bool IsAvailable { get => !string.IsNullOrWhiteSpace(orderId) && !string.IsNullOrWhiteSpace(sequence) && mw > 0 && quality > 0 && purity != 0; }// 判断一条数据是否有效，需要它的orderId和Sequence不为空，分子量质量不小于0，纯度不等于0

        public double Purity { get => purity; set => purity = value; }
        public string PurityString
        {
            get
            {
                switch (purity)
                {
                    case -1: return "Desalt";
                    case -2: return "Crude";
                    default: return $"{(purity < 1 ? purity * 100 : purity)}%";
                }
            }
            set
            {
                value = value == null ? "" : value.ToLower();
                if (value == "desalt") purity = -1;
                else if (value == "crude") purity = -2;
                else purity = getMaxValue(value);
            }
        }

        private double getNum(object value)
        {
            switch (value == null ? "" : value.GetType().Name)
            {
                case "Double":
                case "Int32":
                case "Int64":
                case "Single":
                    return (double)value;
                case "String":
                    double.TryParse((string)value, out double l);
                    return l;
                default:return -1;
            }
        }

        private string intiComments(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // NnConfig
            NnConfig config = NnConfig._nnConfig;
            foreach(NnTfaFlg flg in config.TfaFlgs)
            {
                if(Regex.IsMatch(value,flg.Name,RegexOptions.IgnoreCase))
                {
                    tfaflg |= (1 << flg.Flg);
                }
            }
            return value;
        }

        // 获取字符串中所有数字的和
        private double getSumValue(string str)
        {
            str += '\0';
            double value = 0;
            int index = 0;
            for (int len = 0; len < str.Length; ++len)
            {
                char c = str[len];
                if ((c < '0' || c > '9') && c != '.')
                {
                    if (len > index)
                    {
                        double d = 0;
                        value += (double.TryParse(str.Substring(index, len - index), out d)) ? d : 0;
                    }
                    index = len + 1;
                }
            }
            return value;
        }
        // 获得字符串中最大的数字
        private double getMaxValue(string str)
        {
            str += '\0';
            double value = 0;
            int index = 0;
            for (int len = 0; len < str.Length; ++len)
            {
                char c = str[len];
                if ((c < '0' || c > '9') && c != '.')
                {
                    if (len > index)
                    {
                        double d = 0;
                        value = (double.TryParse(str.Substring(index, len - index), out d) && d > value) ? d : value;
                    }
                    index = len + 1;
                }
            }
            return value;
        }

        internal object[] GetObjects()
        {
            return new object[] { DateTime.Now.ToString(), WorkNo, OrderId, Sequence, Purity, Mw, Modification, Comments };
        }
    }

    class NnStockInfo
    {
        private ArrayList m_list;
        private NnPolypeptide newPolypeptide;
        private double quality = 0;// 质量的和
        private double quality_er = 0;// 错误的质量和
        private bool IsHaveModError = false;// 是否包含有可能有错误的库存

        public bool QualityFlg { get => quality > newPolypeptide.Quality; }// 这个值如果没有问题的质量足够，就为true

        public NnStockInfo(NnPolypeptide p)
        {
            m_list = new ArrayList();
            newPolypeptide = p;
        }

        public bool IsAvailable { get => quality + quality_er > 0; }// 如果质量大于0，说明这条库存有效

        public NnColorFlg ColorFlg
        {
            get
            {
                if (IsHaveModError) return NnColorFlg.Modification;// 如果质量不够，而且有有可能错误的订单，则返回modification
                if (!QualityFlg) return NnColorFlg.Quality;// 如果质量不够，但是没有有可能有错误的订单，就返回质量不足
                return NnColorFlg.Usual;
            }
        }

        public bool Add(NnStock stock)
        {
            if (stock == null || !stock.IsAvailable) return false;// 如果stock为空或者无效，则返回
            // TODO 正式上线这里不要注释掉
            //if (stock.OrderId == newPolypeptide.OrderId) return false;// 如果查到的是自己，直接返回,也不对，自己不可能有库存啊
                
            int flg = stock.Match(newPolypeptide);

            if (flg < 0) return false;// 如果不匹配，则返回

            if (flg == 0)// 纯度足够
                quality += stock.Quality;
            else if (flg == 1)// 纯度不够
                quality += stock.Quality / 3;
            else
            {// 未转盐或修饰可能有问题
                quality_er += stock.Quality / 3;
                IsHaveModError = true;
            }
            _add(stock);
            return true;
        }

        // 保证按顺序插入列表
        private void _add(NnStock stock)
        {
            for (int i = 0; i < m_list.Count; ++i)
            {
                if (stock.OrderId == (m_list[i] as NnStock).OrderId) return;// 如果是重复的，直接返回
                if (stock.Quality > (m_list[i] as NnStock).Quality)
                {
                    m_list.Insert(i, stock);
                    return;
                }
            }
            m_list.Add(stock);
        }

        // 获取当前库存的字符串
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            bool fast = true;
            foreach (NnStock sk in m_list)
            {
                if (QualityFlg && sk.Flg != 0 && (sk.Flg & 1) != 1) continue;// 如果质量够，则只要没有问题的库存（纯度不够的也算是没有问题啊）// 怎样的订单没有问题？flg=0的和(sk.Flg & 1)=1的
                sb.Append(fast ? "" : " \\  ").Append(sk.ToString());
                fast = false;
            }

            return sb.ToString();
        }
    }

    class NnStock : NnPolypeptide
    {
        public DateTime Date { get; set; }// 库存日期
        public string Packages { get; set; }// 袋
        public string Coordinate { get; set; }// 坐标

        // 注意，在调用Flg之前一定要调用Match函数，使之与新单匹配，否则无法得到flg
        public int Flg { get; set; }

        public NnStock(string orderId, string sequence) : base(orderId, sequence) { Flg = -1; }
        public NnStock() : base("", "")
        {
            Flg = -1;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(OrderId);
            builder.Append(" ").Append(Date.ToShortDateString()).Append(" ").Append(Quality).Append(" ")
                .Append(PurityString).Append(" ").Append(Coordinate).Append(" ").Append((Flg & 4) == 0 ? "" : "未转盐").Append((Flg & 2) == 0 ? "" : " " + Modification);
            return builder.ToString();
        }

        /**
         * 返回两个多肽信息的匹配程度，纯度，修饰，转盐信息
         * -1 不匹配
         * 第1位 为0纯度合格，为1纯度不够，注意，如果需要的为crude，则直接返回-1
         * 第2位 为0修饰相同，为1修饰可能不同
         * 第3位 为0转盐相同，为1未转盐，注意，转盐信息有很大差别，则直接返回-1
         * 转盐不同，在添加库存的时候注意区分库存未转盐和转盐不同
         * 
         * 这里先看修饰是否想同，不同再匹配其分子量
         */
        public int Match(NnPolypeptide b)
        {
            int flg = 0;
            // 匹配修饰
            if (Modification.ToLower() != b.Modification.ToLower())// 如果修饰不同，判断分子量
            {
                double abs = Math.Abs(Mw - b.Mw);
                if ((abs < 0.7) || (Math.Abs(abs - 18) < 0.7))
                    flg |= 2;
                else return -1;// 修饰不同，分子量也有很大差别，直接返回
            }
            // 匹配纯度
            if (b.Purity < 0 && b.Purity < Purity) return -1;
            else if (b.Purity > Purity) flg |= 1;// 纯度不够不用标注出来了
            // 匹配转盐
            if (b.Tfaflg != Tfaflg)
                if (Tfaflg == 0) flg |= 4;// 如果新单要求转盐而库存未转盐，则返回库存未转盐
                else return -1;// 否则就是转盐不合格，直接退出
            Flg = flg;
            return flg;
        }

        internal void InitStockByDb(OleDbDataReader reader)
        {
            OrderId = NnReader.GetStringFromDb(reader, "history.orderId");
            Sequence = NnReader.GetStringFromDb(reader, "sequence");
            Quality = NnReader.GetDoubleFromDb(reader, "quality");
            Mw = NnReader.GetDoubleFromDb(reader, "history.mw");
            Purity = NnReader.GetDoubleFromDb(reader, "history.purity");
            Modification = NnReader.GetStringFromDb(reader, "modification");
            Comments = NnReader.GetStringFromDb(reader, "history.comments");
            Date = NnReader.GetDateTimeFromDb(reader, "_date");
            WorkNo = NnReader.GetIntFromDb(reader, "history.workNo");
            Coordinate = NnReader.GetStringFromDb(reader, "coordinate");
        }
    }

    // 单元格颜色枚举
    enum NnColorFlg
    {
        Quality,// 量不足
        Modification,// 修饰可能有问题
        Usual// 正常
        //Tfa// 转盐可能有问题
    }
}
