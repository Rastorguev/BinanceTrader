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

        public async Task<IReadOnlyList<TraderConfig>> GetConfigsAsync()
        {
            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(_connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference("TraderConfigs.json").NotNull();

            string content;
            using (var result = await blob.OpenReadAsync())
            {
                byte[] bytes = new byte[result.Length];
                result.Read(bytes, 0, (int)result.Length);
                content = System.Text.Encoding.Default.GetString(bytes);
            }

            var configs = JsonConvert.DeserializeObject<IReadOnlyList<TraderConfig>>(content).NotNull();

            return configs;
        }
    }
}