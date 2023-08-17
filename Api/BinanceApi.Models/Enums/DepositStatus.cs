using System.ComponentModel;
using Newtonsoft.Json;

namespace BinanceApi.Models.Enums
{
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum DepositStatus
    {
        Unknown = -1000,
        Pending = 0,
        Success = 1
    }
}