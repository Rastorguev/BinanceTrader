using System.Configuration;
using System.IO;
using BinanceTrader.Tools;

namespace BinanceTrader.Api
{
    public interface IBinanceKeyProvider
    {
        BinanceKeys GetKeys();
    }

    public class MockKeyProvider : IBinanceKeyProvider
    {
        public BinanceKeys GetKeys()
        {
            return new BinanceKeys();
        }
    }


    public class BinanceKeyProvider : IBinanceKeyProvider
    {
        private readonly string _keysFilePath;

        public BinanceKeyProvider(string keysFilePath) => _keysFilePath = keysFilePath;

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

    public class BinanceKeys
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
    }
}