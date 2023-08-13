using System.ComponentModel;
using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Enums
{
    [JsonConverter(typeof(DefaultValueEnumConverter))]
    [DefaultValue(Unknown)]
    public enum ExecutionType
    {
        Unknown = -1000,
        New,
        Canceled,
        Replaced,
        Rejected,
        Trade,
        Expired
    }
}