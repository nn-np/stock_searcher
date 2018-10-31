using System.Configuration;
using System.IO;

namespace nnns
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
        [ConfigurationProperty("aminoAcids", IsDefaultCollection = false)]
        public NnAminoAcids AminoAcids { get => base["aminoAcids"] as NnAminoAcids; }

        [ConfigurationProperty("tfaflgs")]
        public NnTfaFlgs TfaFlgs { get => base["tfaflgs"] as NnTfaFlgs; }
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
        [ConfigurationProperty("name", IsKey = true)]
        public string Name { get => this["name"] as string; }
        [ConfigurationProperty("flg")]
        private string flg { get => this["flg"] as string; }

        public int Flg
        {
            get
            {
                int i;
                if (!int.TryParse(flg, out i)) i = 0;
                return i;
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
                double d;
                if (!double.TryParse(mw, out d)) d = 0;
                return d;
            }
        }
    }
}
