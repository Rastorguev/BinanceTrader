using System.ComponentModel;
using Newtonsoft.Json;

namespace BinanceApi.Models.Enums
{
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum WithdrawStatus
    {
        Unknown = -1000,
        EmailSent = 0,
        Cancelled = 1,
        AwaitingApproval = 2,
        Rejected = 3,
        Processing = 4,
        Failure = 5,
        Completed = 6
    }
}