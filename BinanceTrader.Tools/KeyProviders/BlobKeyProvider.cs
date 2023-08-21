using System.Text;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders;

public class BlobKeyProvider : IKeyProvider
{
    [NotNull]
    private readonly string _connectionString;

    public BlobKeyProvider([NotNull] string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<BinanceKeySet>> GetKeysAsync()
    {
        const string containerName = "configs";
        var account = CloudStorageAccount.Parse(_connectionString).NotNull();
        var client = account.CreateCloudBlobClient().NotNull();
        var container = client.GetContainerReference(containerName).NotNull();
        var blob = container.GetBlobReference("Keys.json").NotNull();

        string content;
        using (var result = await blob.OpenReadAsync())
        {
            var bytes = new byte[result.Length];
            result.Read(bytes, 0, (int)result.Length);
            content = Encoding.Default.GetString(bytes);
        }

        var keys = JsonConvert.DeserializeObject<IReadOnlyList<BinanceKeySet>>(content).NotNull();

        return keys;
    }
}