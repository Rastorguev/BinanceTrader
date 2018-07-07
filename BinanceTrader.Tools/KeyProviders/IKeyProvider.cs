using System.Configuration;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders
{
    public interface IKeyProvider
    {
        [NotNull]
        BinanceKeys GetKeys([NotNull] string keySetName);
    }

    public class ConfigFileKeyProvider : IKeyProvider
    {
        public BinanceKeys GetKeys(string keySetName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var keysPath = Path.Combine(
                Path.GetDirectoryName(assembly.Location).NotNull(),
                "Configs",
                $"Keys_{keySetName}.config");

            var configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = keysPath
            };

            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            if (config.HasFile == false)
            {
                throw new FileNotFoundException("Keys config file not found", keysPath);
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
        [NotNull] private readonly IConnectionStringsProvider _connectionStringsProvider;

        public BlobKeyProvider(
            [NotNull] IConnectionStringsProvider connectionStringsProvider)
        {
            _connectionStringsProvider = connectionStringsProvider;
        }

        public BinanceKeys GetKeys(string keySetName)
        {
            var connectionString = _connectionStringsProvider.GetConnectionString(keySetName);

            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference($"Keys_{keySetName}.json").NotNull();

            string content;
            using (var reader = new StreamReader(blob.OpenRead().NotNull()))
            {
                content = reader.ReadToEnd();
            }

            var keys = JsonConvert.DeserializeObject<BinanceKeys>(content).NotNull();

            return keys;
        }
    }

    public class BinanceKeys
    {
        public string Api { get; set; }
        public string Secret { get; set; }
    }
}