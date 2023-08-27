using System.Text;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace BinanceTrader.Tools.KeyProviders;

public class BlobConfigProvider : ITraderConfigProvider
{
    private readonly string _connectionString;

    public BlobConfigProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<TraderConfig>> GetConfigsAsync()
    {
        const string containerName = "configs";
        var account = CloudStorageAccount.Parse(_connectionString);
        var client = account.CreateCloudBlobClient();
        var container = client.GetContainerReference(containerName);
        var blob = container.GetBlobReference("TraderConfigs.json");

        string content;
        using (var result = await blob.OpenReadAsync())
        {
            var bytes = new byte[result.Length];
            result.Read(bytes, 0, (int)result.Length);
            content = Encoding.Default.GetString(bytes);
        }

        var configs = JsonConvert.DeserializeObject<IReadOnlyList<TraderConfig>>(content);

        return configs;
    }
}