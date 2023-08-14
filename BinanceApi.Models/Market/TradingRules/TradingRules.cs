using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Market.TradingRules
{
    public interface ITradingRules
    {
        string Symbol { get; set; }
        SymbolStatus Status { get; set; }
        string BaseAsset { get; set; }
        int BaseAssetPrecision { get; set; }
        string QuoteAsset { get; set; }
        int QuotePrecision { get; set; }
        IEnumerable<string> OrderTypes { get; set; }
        bool IcebergAllowed { get; set; }
        decimal MinPrice { get; }
        decimal MaxPrice { get; }
        decimal TickSize { get; }
        decimal MinQty { get; }
        decimal MaxQty { get; }
        decimal StepSize { get; }
        decimal MinNotional { get; }
    }

    public class TradingRules : ITradingRules
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("status")]
        public SymbolStatus Status { get; set; }

        [JsonProperty("baseAsset")]
        public string BaseAsset { get; set; }

        [JsonProperty("baseAssetPrecision")]
        public int BaseAssetPrecision { get; set; }

        [JsonProperty("quoteAsset")]
        public string QuoteAsset { get; set; }

        [JsonProperty("quotePrecision")]
        public int QuotePrecision { get; set; }

        [JsonProperty("orderTypes")]
        public IEnumerable<string> OrderTypes { get; set; }

        [JsonProperty("icebergAllowed")]
        public bool IcebergAllowed { get; set; }

        [JsonProperty("filters")]
        public IEnumerable<Filter> Filters { get; set; }

        public decimal MinPrice => Filters.First(f => f.FilterType == ExchangeFilterType.PriceFilter).MinPrice;
        public decimal MaxPrice => Filters.First(f => f.FilterType == ExchangeFilterType.PriceFilter).MaxPrice;
        public decimal TickSize => Filters.First(f => f.FilterType == ExchangeFilterType.PriceFilter).TickSize;
        public decimal MinQty => Filters.First(f => f.FilterType == ExchangeFilterType.LotSize).MinQty;
        public decimal MaxQty => Filters.First(f => f.FilterType == ExchangeFilterType.LotSize).MaxQty;
        public decimal StepSize => Filters.First(f => f.FilterType == ExchangeFilterType.LotSize).StepSize;
        public decimal MinNotional => Filters.First(f => f.FilterType == ExchangeFilterType.Notional).MinNotional;
    }
}