using Newtonsoft.Json;

namespace BinanceApi.Models.UserStream
{
    public class UserStreamInfo
    {
        [JsonProperty("listenKey")]
        public string ListenKey { get; set; }
    }
}