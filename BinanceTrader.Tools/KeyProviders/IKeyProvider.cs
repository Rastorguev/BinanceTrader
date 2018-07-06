using System.Configuration;
using System.IO;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders
{
    public interface IKeyProvider
    {
        [NotNull]
        BinanceKeys GetKeys();
    }

    public class ConfigFileKeyProvider : IKeyProvider
    {
        [NotNull] private readonly string _keysFilePath;

        public ConfigFileKeyProvider([NotNull] string connectionString)
        {
            _keysFilePath = connectionString;
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

            var setting = config.AppSettings.NotNull().Settings.NotNull();
            return new BinanceKeys
            {
                Api = setting["Api"].NotNull().Value,
                Secret = setting["Secret"].NotNull().Value
            };
        }
    }

    public class BlobKeyProvider : IKeyProvider
    {
        [NotNull] private readonly string _keySetName;

        public BlobKeyProvider([NotNull] string keySetName)
        {
            _keySetName = keySetName;
        }

        public BinanceKeys GetKeys()
        {
            var connectionStringsProvider = new ConnectionStringsProvider();
            var connectionString = connectionStringsProvider.GetConnectionString(_keySetName);

            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference($"Keys_{_keySetName}.json").NotNull();

            string content;
            using (var reader = new StreamReader(blob.OpenRead().NotNull()))
            {
                content = reader.ReadToEnd();
            }

            var keys = JsonConvert.DeserializeObject<BinanceKeys>(content);

            return keys;
        }
    }

    public class BinanceKeys
    {
        public string Api { get; set; }
        public string Secret { get; set; }
    }
}