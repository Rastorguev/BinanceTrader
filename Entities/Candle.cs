﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Entities
{
    public class CandlesChart
    {
        [NotNull]
        [ItemNotNull]
        public List<Candle> Candles { get; set; } = new List<Candle>();
    }

    public class Candle
    {
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal Volume { get; set; }

        public long NumberOfTrades { get; set; }
        //public decimal QuoteAssetVolume { get; set; }
        //public decimal TakerBuyBasesAssetVolume { get; set; }
        //public decimal TakerBuyQuoteAssetVolume { get; set; }
    }
}