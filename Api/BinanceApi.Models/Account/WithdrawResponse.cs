using Newtonsoft.Json;

namespace BinanceApi.Models.Account
{
    public class WithdrawResponse
    {
        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}