using System;
using System.Linq;
using System.Threading.Tasks;
using BinanceTrader.Api;
using BinanceTrader.Entities;

namespace BinanceTrader
{
    public class PriceMonitor
    {
        private readonly BinanceApi _api;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;

        public PriceMonitor(
            BinanceApi api,
            string baseAsset,
            string quoteAsset)
        {
            _api = api;
            _baseAsset = baseAsset;
            _quoteAsset = quoteAsset;
        }

        public PriceState State { get; set; }

        public void Start()
        {
            while (true)
            {
                var candles =
                    new Candles(
                        _api.GetCandles(_baseAsset, _quoteAsset, "1m").Result.OrderBy(c => c.OpenTime).ToList());

                var ma7 = decimal.Round(CalculateAveragePrice(candles, 7), 8);
                var ma25 = decimal.Round(CalculateAveragePrice(candles, 25), 8);

                var currentState = PriceState.Unknown;

                if (ma7 > ma25)
                {
                    currentState = PriceState.Rising;
                }
                else
                {
                    currentState = PriceState.Falling;
                }

                if (State != currentState)
                {
                    State = currentState;
                }

                Console.WriteLine(State);
                Console.WriteLine($"Time\t: {DateTime.Now.ToLongTimeString()}");
                Console.WriteLine($"MA(7)\t: {ma7}");
                Console.WriteLine($"MA(25)\t: {ma25}");
                Console.WriteLine();

                Task.Delay(TimeSpan.FromMinutes(1)).Wait();
            }
        }

        private decimal CalculateAveragePrice(Candles candles, int n)
        {
            var range = candles.GetRange(candles.Count - n, n);

            var av = range.Average(c => c.ClosePrice);

            return av;
        }

        public enum PriceState
        {
            Unknown,
            Rising,
            Falling,
            Changing
        }
    }
}