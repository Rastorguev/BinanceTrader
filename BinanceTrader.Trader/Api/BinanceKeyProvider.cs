﻿using System.Configuration;
using System.IO;

namespace BinanceTrader.Api
{
    public interface IBinanceKeyProvider
    {
        BinanceKeys GetKeys();
    }

    public class BinanceKeyProvider : IBinanceKeyProvider
    {
        private readonly string _keysFilePath;

        public BinanceKeyProvider(string keysFilePath)
        {
            _keysFilePath = keysFilePath;
        }

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

            var setting = config.AppSettings.Settings;
            return new BinanceKeys {ApiKey = setting["ApiKey"].Value, SecretKey = setting["SecretKey"].Value};
        }
    }

    public class BinanceKeys
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
    }
}