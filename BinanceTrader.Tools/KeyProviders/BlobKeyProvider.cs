using System.Text;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders;

public class BlobKeyProvider : IKeyProvider
{
    private readonly string _connectionString;

    public BlobKeyProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<BinanceKeySet>> GetKeysAsync()
    {
        const string containerName = "configs";
        var account = CloudStorageAccount.Parse(_connectionString);
        var client = account.CreateCloudBlobClient();
        var container = client.GetContainerReference(containerName);
        var blob = container.GetBlobReference("Keys.json");

        string content;
        using (var result = await blob.OpenReadAsync())
        {
            var bytes = new byte[result.Length];
            result.Read(bytes, 0, (int)result.Length);
            content = Encoding.Default.GetString(bytes);
        }

        var keys = JsonConvert.DeserializeObject<IReadOnlyList<BinanceKeySet>>(content);

        return keys;
    }
}