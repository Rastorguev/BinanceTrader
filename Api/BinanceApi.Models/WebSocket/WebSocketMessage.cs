using Newtonsoft.Json;

namespace BinanceApi.Models.WebSocket
{
    public class WebSocketMessage : IWebSocketMessage
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }
    }
}