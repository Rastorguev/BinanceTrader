using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Market.TradingRules
{
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum SymbolStatus
    {
        Unknown = -1000,

        [EnumMember(Value = "PRE_TRADING")]
        PreTrading,

        [EnumMember(Value = "TRADING")]
        Trading,

        [EnumMember(Value = "POST_TRADING,")]
        PostTrading,

        [EnumMember(Value = "END_OF_DAY")]
        EndOfDay,

        [EnumMember(Value = "HALT")]
        Halt,

        [EnumMember(Value = "AUCTION_MATCH")]
        AuctionMatch,

        [EnumMember(Value = "BREAK")]
        Break
    }
}