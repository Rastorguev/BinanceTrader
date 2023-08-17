using System.ComponentModel;
using Newtonsoft.Json;

namespace BinanceApi.Models.Enums
{
    /// <summary>
    ///     Different Time in force of an order.
    /// </summary>
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum TimeInForce
    {
        Unknown = 1000,
        GTC,
        IOC,
        FOK
    }
}