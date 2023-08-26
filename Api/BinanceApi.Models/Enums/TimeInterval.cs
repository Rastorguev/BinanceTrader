using System.ComponentModel;
using Newtonsoft.Json;

namespace BinanceApi.Models.Enums
{
    /// <summary>
    ///     Time interval for the candlestick.
    /// </summary>
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum TimeInterval
    {
        Unknown = -1000,

        [Description("1s")]
        Seconds_1,

        [Description("1m")]
        Minutes_1,

        [Description("3m")]
        Minutes_3,

        [Description("5m")]
        Minutes_5,

        [Description("15m")]
        Minutes_15,

        [Description("30m")]
        Minutes_30,

        [Description("1h")]
        Hours_1,

        [Description("2h")]
        Hours_2,

        [Description("4h")]
        Hours_4,

        [Description("6h")]
        Hours_6,

        [Description("8h")]
        Hours_8,

        [Description("12h")]
        Hours_12,

        [Description("1d")]
        Days_1,

        [Description("3d")]
        Days_3,

        [Description("1w")]
        Weeks_1,

        [Description("1M")]
        Months_1
    }
}