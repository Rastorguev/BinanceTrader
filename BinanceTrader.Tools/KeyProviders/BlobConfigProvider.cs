using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders
{
    public class BlobConfigProvider : ITraderConfigProvider
    {
        [NotNull] private readonly string _connectionString;

        public BlobConfigProvider([NotNull] string connectionString) => _connectionString = connectionString;

        public IReadOnlyList<TraderConfig> GetConfigs()
        {
            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(_connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference("TraderConfigs.json").NotNull();

            string content;
            using (var reader = new StreamReader(blob.OpenRead().NotNull()))
            {
                content = reader.ReadToEnd();
            }

            var configs = JsonConvert.DeserializeObject<IReadOnlyList<TraderConfig>>(content).NotNull();

            return configs;
        }
    }
}