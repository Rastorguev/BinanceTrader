using System.Globalization;
using Microsoft.Azure.WebJobs;

namespace BinanceTrader.WebJob
{
    internal class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);
            host.Call(typeof(Functions).GetMethod("Start"));
            host.RunAndBlock();
        }
    }
}