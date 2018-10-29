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
}
