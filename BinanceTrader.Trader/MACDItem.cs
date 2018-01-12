using BinanceTrader.Entities;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class MACDItem
    {
        public Candle Candle { get; set; }
        public decimal ShortSMA { get; set; }
        public decimal ShortEMA { get; set; }
        public decimal LongSMA { get; set; }
        public decimal LongEMA { get; set; }
        public decimal MACD { get; set; }
        public decimal Signal { get; set; }

        public decimal? MACDHist => MACD - Signal;
    }

    public static class MACDItemExtensions
    {
        public static MACDHistType GetMACDHistType([NotNull] this MACDItem item)
        {
            if (item.MACDHist == null)
            {
                return MACDHistType.Undefined;
            }
            if (item.MACDHist.Value > 0)
            {
                return MACDHistType.Positive;
            }
            if (item.MACDHist.Value < 0)
            {
                return MACDHistType.Negative;
            }
            if (item.MACDHist.Value == 0)
            {
                return MACDHistType.Neutral;
            }

            return MACDHistType.Undefined;
        }
    }
}