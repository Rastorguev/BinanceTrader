using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var candlesProvider=new CandlesProvider(binanceClient);


            var tests=new TradyTests(candlesProvider);
            tests.Execute().Wait();

            //var tests = new StrategiesTests(binanceClient);
            //tests.CompareStrategies().Wait();

         

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).Wait();
        }
    }
}