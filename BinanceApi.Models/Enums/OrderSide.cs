using System.ComponentModel;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Enums
{
    /// <summary>
    ///     Different sides of an order.
    /// </summary>
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum OrderSide
    {
        Unknown = -1000,
        Buy,
        Sell
    }
}