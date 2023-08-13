using System.ComponentModel;
using System.Runtime.Serialization;
using Binance.API.Csharp.Client.Models.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Binance.API.Csharp.Client.Models.Market.TradingRules
{
    public class Filter
    {
        [JsonProperty("filterType")]
        public ExchangeFilterType FilterType { get; set; }

        [JsonProperty("minPrice")]
        public decimal MinPrice { get; set; }

        [JsonProperty("maxPrice")]
        public decimal MaxPrice { get; set; }

        [JsonProperty("tickSize")]
        public decimal TickSize { get; set; }

        [JsonProperty("minQty")]
        public decimal MinQty { get; set; }

        [JsonProperty("maxQty")]
        public decimal MaxQty { get; set; }

        [JsonProperty("stepSize")]
        public decimal StepSize { get; set; }

        [JsonProperty("minNotional")]
        public decimal MinNotional { get; set; }

        [JsonProperty("multiplierUp")]
        public decimal MultiplierUp { get; set; }

        [JsonProperty("multiplierDown")]
        public decimal MultiplierDown { get; set; }

        [JsonProperty("avgPriceMins")]
        public int AvgPriceMins { get; set; }

        [JsonProperty("applyToMarket")]
        public bool ApplyToMarket { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }
        
        [JsonProperty("maxNumOrders")]
        public int MaxNumOrders { get; set; }

        [JsonProperty("maxNumAlgoOrders")]
        public int MaxNumAlgoOrders { get; set; }
        
        [JsonProperty("maxNumIcebergOrders")]
        public int MaxNumIcebergOrders { get; set; }

        [JsonProperty("maxPosition")]
        public decimal MaxPosition { get; set; }
    }

    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum ExchangeFilterType
    {
        Unknown = -1000,

        [EnumMember(Value = "PRICE_FILTER")]
        PriceFilter,

        [EnumMember(Value = "PERCENT_PRICE")]
        PercentPrice,

        [EnumMember(Value = "LOT_SIZE")]
        LotSize,

        [EnumMember(Value = "MIN_NOTIONAL")]
        MinNotional,

        [EnumMember(Value = "ICEBERG_PARTS")]
        IcebergParts,

        [EnumMember(Value = "MARKET_LOT_SIZE")]
        MarketLotSize,

        [EnumMember(Value = "MAX_NUM_ORDERS")]
        MaxNumOrders,

        [EnumMember(Value = "MAX_NUM_ALGO_ORDERS")]
        MaxNumAlgoOrders, 
        
        [EnumMember(Value = "MAX_NUM_ICEBERG_ORDERS")]
        MaxNumIcebergOrders, 
        
        [EnumMember(Value = "MAX_POSITION")]
        MaxPosition,

        [EnumMember(Value = "EXCHANGE_MAX_NUM_ORDERS")]
        ExchangeMaxNumOrders,

        [EnumMember(Value = "EXCHANGE_MAX_NUM_ALGO_ORDERS")]
        ExchangeMaxNumAlgoOrders
    }
}