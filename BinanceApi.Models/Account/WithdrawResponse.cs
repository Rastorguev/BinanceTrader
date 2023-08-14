using Newtonsoft.Json;

namespace Binance.API.Csharp.Client.Models.Account
{
    public class WithdrawResponse
    {
        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}