using System;
using System.Collections.Generic;
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

                var stats = new List<Stat>();

                for (var i = 25; i <= candles.Count; i++)
                {
                    var c = candles.GetRange(0, i);

                    var ma_7 = decimal.Round(CalculateAveragePrice(c, 7), 8);
                    var ma_25 = decimal.Round(CalculateAveragePrice(c, 25), 8);

                    var diff = (ma_7 - ma_25) * 100 / ma7;

                    var current = c.Last();

                    var stat = new Stat
                    {
                        Time = current.CloseTime,
                        MA7 = ma_7,
                        MA25 = ma_25,
                        Diff = diff
                    };

                    stats.Add(stat);
                }

                var st = stats.OrderBy(s => s.Diff).ToList();

                var ch = stats.OrderBy(s => Math.Abs(s.Diff)).ToList();

                var min = st.First();
                var max = st.Last();

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

        public class Stat
        {
            public DateTime Time { get; set; }
            public decimal MA7 { get; set; }
            public decimal MA25 { get; set; }
            public decimal Diff { get; set; }
        }

        private decimal CalculateAveragePrice(List<Candle> candles, int n)
        {
            var range = candles.GetRange(candles.Count - n, n);
            return range.Average(c => c.ClosePrice);
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