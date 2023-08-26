using System;
using BinanceApi.Models.Extensions;

namespace BinanceApi.Models.Market
{
    public class Candlestick
    {
        public long OpenUnixTime { get; set; }
        public DateTime OpenLocalTime => OpenUnixTime.GetLocalTime();
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public long CloseUnixTime { get; set; }
        public DateTime CloseLocalTime => CloseUnixTime.GetLocalTime();
        public decimal QuoteAssetVolume { get; set; }
        public int NumberOfTrades { get; set; }
        public decimal TakerBuyBaseAssetVolume { get; set; }
        public decimal TakerBuyQuoteAssetVolume { get; set; }
    }
}