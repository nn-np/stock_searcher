using System.Configuration;
using System.IO;

namespace nnns.data
{
    class NnConfiguration
    {
        private Configuration configuration;

        public NnConfiguration() => configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public NnConfiguration(string url)
        {
            if (!File.Exists(url)) File.Create(url).Close();
            configuration = ConfigurationManager.OpenExeConfiguration(url);
            File.Delete(url);
        }

        public void set(string key,object value)
        {
            if (configuration.AppSettings.Settings[key] == null)
                configuration.AppSettings.Settings.Add(key, value.ToString());
            else
                configuration.AppSettings.Settings[key].Value = value.ToString();
        }

        public string getString(string key, string defaut = null) => configuration.AppSettings.Settings[key] == null ? defaut : configuration.AppSettings.Settings[key].Value;

        public int? getInt(string key, int? defaut = null) => configuration.AppSettings.Settings[key] == null ? defaut : int.Parse(configuration.AppSettings.Settings[key].Value);

        public double? getDouble(string key, double? defaut = null) => configuration.AppSettings.Settings[key] == null ? defaut : double.Parse(configuration.AppSettings.Settings[key].Value);

        public void save() => configuration.Save();

        public void saveAs(string url) => configuration.SaveAs(url + ".config");

        public void refresh() => ConfigurationManager.RefreshSection(configuration.AppSettings.File);
    }

    class NnConfig : ConfigurationSection
    {
        private static NnConfig nnConfig = null;
        private static readonly object SynObject = new object();

        private NnConfig() { }

        internal static NnConfig _nnConfig
        {
            get
            {
                if (nnConfig == null)
                {
                    lock (SynObject)
                    {
                        if (nnConfig == null) nnConfig = ConfigurationManager.GetSection("nnconfig") as NnConfig;
                    }
                }
                return nnConfig;
            }
        }
        
        [ConfigurationProperty("aminoAcids", IsDefaultCollection = false)]
        public NnAminoAcids AminoAcids { get => base["aminoAcids"] as NnAminoAcids; }

        [ConfigurationProperty("tfaflgs")]
        public NnTfaFlgs TfaFlgs { get => base["tfaflgs"] as NnTfaFlgs; }

        [ConfigurationProperty("titles")]
        public NnTitleFlgs TitleFlgs { get => base["titles"] as NnTitleFlgs; }
    }

    // 标题列对应类
    class NnTitleFlgs : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new NnTitleflg();

        protected override object GetElementKey(ConfigurationElement element) => ((NnTitleflg)element).Name;
        
        new public NnTitleflg this[string name] => BaseGet(name) as NnTitleflg;

        public NnTitleflg this[int index] => BaseGet(index) as NnTitleflg;
    }

    class NnTitleflg: ConfigurationElement
    {
        int _column = -1;

        [ConfigurationProperty("name", IsKey = true)]
        public string Name { get => this["name"] as string; }
        [ConfigurationProperty("column")]
        private string column { get => this["column"] as string; }

        public int Flg
        {
            get
            {
                if (_column < 0 && !int.TryParse(column, out _column)) _column = 0;
                return _column;
            }
        }
    }

    // 转盐配置类
    class NnTfaFlgs : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new NnTfaFlg();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NnTfaFlg)element).Name;
        }

        new public NnTfaFlg this[string name] => BaseGet(name) as NnTfaFlg;

        public NnTfaFlg this[int index]
        {
            get { return BaseGet(index) as NnTfaFlg; }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }
    }

    class NnTfaFlg: ConfigurationElement
    {
        int _flg = -1;

        [ConfigurationProperty("name", IsKey = true)]
        public string Name { get => this["name"] as string; }
        [ConfigurationProperty("flg")]
        private string flg { get => this["flg"] as string; }

        public int Flg
        {
            get
            {
                if (_flg < 0 && !int.TryParse(flg, out _flg)) _flg = 0;
                return _flg;
            }
        }
    }
    

    // 氨基酸配置类，这里包括所有20中氨基酸和一些修饰的分子量以及单字母三字母，注意这是一个集合
    class NnAminoAcids : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new NnAminoAcid();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NnAminoAcid)element).Name;
        }

        new public NnAminoAcid this[string name] => BaseGet(name) as NnAminoAcid;

        public NnAminoAcid this[int index]
        {
            get { return BaseGet(index) as NnAminoAcid; }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }
    }
    // 这是单个氨基酸配置类，子节点
    class NnAminoAcid : ConfigurationElement
    {
        double _mw = -1;

        // 氨基酸名称，即三字母
        [ConfigurationProperty("name", IsKey = true)]
        public string Name { get => this["name"] as string; set => this["name"] = value; }
        // 氨基酸单字母
        [ConfigurationProperty("one")]
        public string One { get => this["one"] as string; set => this["one"] = value; }
        // 氨基酸分子量
        [ConfigurationProperty("mw")]
        private string mw { get => this["mw"] as string; }

        public double Mw
        {
            get
            {
                if (_mw < 0 && !double.TryParse(mw, out _mw)) _mw = 0;
                return _mw;
            }
        }
    }
}
