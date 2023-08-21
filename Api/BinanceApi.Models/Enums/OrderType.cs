using System.ComponentModel;
using Newtonsoft.Json;

namespace BinanceApi.Models.Enums
{
    /// <summary>
    ///     Different types of an order.
    /// </summary>
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum OrderType
    {
        Unknown = -1000,
        Limit,
        Market,
        StopLoss,
        StopLossLimit,
        TakeProfit,
        TakeProfitLimit,
        LimitMaker
    }
}