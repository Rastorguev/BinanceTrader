using System.Configuration;
using System.IO;

namespace BinanceTrader.Tools.KeyProviders
{
    public class ConfigFileKeyProvider : IKeyProvider
    {
        private readonly string _keysFilePath;

        public ConfigFileKeyProvider(string keysFilePath) => _keysFilePath = keysFilePath;

        public BinanceKeys GetKeys()
        {
            var configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = _keysFilePath
            };

            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            if (config.HasFile == false)
            {
                throw new FileNotFoundException("Keys config file not found", _keysFilePath);
            }

            var setting = config.AppSettings.NotNull().Settings.NotNull();
            return new BinanceKeys
            {
                ApiKey = setting["ApiKey"].NotNull().Value,
                SecretKey = setting["SecretKey"].NotNull().Value
            };
        }
    }
}