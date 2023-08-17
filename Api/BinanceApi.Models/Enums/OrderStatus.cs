using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Enums
{
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum OrderStatus
    {
        Unknown = -1000,
        New,

        [EnumMember(Value = "PARTIALLY_FILLED")]
        PartiallyFilled,
        Filled,
        Canceled,

        [EnumMember(Value = "PENDING_CANCEL")]
        PendingCancel,
        Rejected,
        Expired
    }
}