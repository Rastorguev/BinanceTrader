using System.Globalization;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;

namespace BinanceTrader.WebJob;

internal class Program
{
    private static async Task Main()
    {
        ServicePointManager.DefaultConnectionLimit = 10;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var builder = new HostBuilder();
        builder.UseEnvironment("development");
        builder.ConfigureWebJobs(b => { b.AddAzureStorageCoreServices(); });
        var host = builder.Build();
        using (host)
        {
            var jobHost = host.Services.GetService(typeof(IJobHost)) as JobHost;
            await jobHost.CallAsync("Start");
            await host.RunAsync();
        }
    }
}