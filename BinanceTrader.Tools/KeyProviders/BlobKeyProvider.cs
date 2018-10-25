using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders
{
    public class BlobKeyProvider : IKeyProvider
    {
        [NotNull] private readonly string _connectionString;

        public BlobKeyProvider([NotNull] string connectionString) =>
            _connectionString = connectionString;

        public IReadOnlyList<BinanceKeySet> GetKeys()
        {
            const string containerName = "configs";
            var account = CloudStorageAccount.Parse(_connectionString).NotNull();
            var client = account.CreateCloudBlobClient().NotNull();
            var container = client.GetContainerReference(containerName).NotNull();
            var blob = container.GetBlobReference("Keys.json").NotNull();

            string content;
            using (var reader = new StreamReader(blob.OpenRead().NotNull()))
            {
                content = reader.ReadToEnd();
            }

            var keys = JsonConvert.DeserializeObject<IReadOnlyList<BinanceKeySet>>(content).NotNull();

            return keys;
        }
    }
}