using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/**
 * 多肽类
 * crude为 -2，desalt为 -1
 */
namespace nnns.data
{
    class NnPolypeptide
    {
        private readonly string orderId;// orderId
        private long workNo;// worknumber
        private double purity;// 纯度
        private double mw;// 分子量
        private double quality;// 质量（可能是需要的量，也可能时库存量）
        private readonly string sequence;// 序列
        private string modification;// 修饰
        private string comments;// 备注
        private int tfaflg;// 转盐信息

        public NnPolypeptide(string orderId, string sequence)
        {
            this.orderId = (orderId ?? "").Contains('-') ? orderId : "";
            this.sequence = Regex.Replace(sequence ?? "", @"\s", "").ToUpper();
        }

        public string OrderId { get => orderId; }
        public string Sequence { get => sequence; }
        public long WorkNo { get => workNo; set => workNo = value > 9999999 ? value : -1; }
        public object WorkNoObj { set => WorkNo = getLong(value); }// 如果从excel表中读取的值类型不确定的话，建议使用这个属性，避免报错闪退
        public double Mw { get => mw; set => mw = value; }
        public string MwString { get => mw.ToString(); set => mw = getMaxValue(value); }
        public double Quality { get => quality; set => quality = value; }
        public string QualityString { get => $"{quality}mg"; set => quality = getSumValue(value); }
        public string Modification { get => modification; set => modification = Regex.Replace(value ?? "", @"\s", "").ToLower(); }// TODO  提取修饰信息？，这里要干什么忘记了，想起来了，这里匹配是要忽略大小写
        public string Comments { get => comments; set => comments = intiComments(value); }

        public int Tfaflg { get => tfaflg; }
        public bool IsAvailable { get => !string.IsNullOrEmpty(orderId) && !string.IsNullOrEmpty(sequence) && workNo > 0 && mw > 0 && quality != 0; }

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
                value = value.ToLower();
                if (value == "desalt") purity = -1;
                else if (value == "crude") purity = -2;
                else purity = getMaxValue(value);
            }
        }

        private long getLong(object value)
        {
            switch (value.GetType().Name)
            {
                case "Double":
                case "Int32":
                case "Int64":
                case "Single":
                    return WorkNo = (long)value;
                default:return -1;
            }
        }

        private string intiComments(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // NnConfig是单例模式
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

        public static int Match(NnPolypeptide a,NnPolypeptide b)
        {
            return 0;
        }
    }
}
