using System.IO;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders
{
    public interface ITraderConfigProvider
    {
        [NotNull]
        TraderConfig GetConfig([NotNull] string name);
    }

    public class BlobConfigProvider : ITraderConfigProvider
    {
        [NotNull] private readonly IConnectionStringsProvider _connectionStringsProvider;

        public BlobConfigProvider([NotNull] IConnectionStringsProvider connectionStringsProvider)
        {
            _connectionStringsProvider = connectionStringsProvider;
        }

        public TraderConfig GetConfig(string name)
        {
            var connectionString = _connectionStringsProvider.GetConnectionString(name);

            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference($"TraderConfig_{name}.json").NotNull();

            string content;
            using (var reader = new StreamReader(blob.OpenRead().NotNull()))
            {
                content = reader.ReadToEnd();
            }

            var config = JsonConvert.DeserializeObject<TraderConfig>(content).NotNull();

            return config;
        }
    }
}