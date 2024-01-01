using System;
using BinanceApi.Models.Extensions;
using Newtonsoft.Json;

namespace BinanceApi.Models.Market
{
    public class Candlestick
    {
        [JsonProperty("openTime")]
        public long OpenUnixTime { get; set; }

        [JsonProperty("closeTime")]
        public long CloseUnixTime { get; set; }

        public DateTime OpenLocalTime => OpenUnixTime.GetLocalTime();
        public DateTime CloseLocalTime => CloseUnixTime.GetLocalTime();
        public decimal Amplitude => (High - Low) / Low * 100;
        public decimal Change => (Close - Open) / Open * 100;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal QuoteAssetVolume { get; set; }
        public int NumberOfTrades { get; set; }
        public decimal TakerBuyBaseAssetVolume { get; set; }
        public decimal TakerBuyQuoteAssetVolume { get; set; }
    }
}