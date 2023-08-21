using System.Collections.Generic;
using BinanceApi.Models.Market;

namespace BinanceApi.Models.WebSocket
{
    public class DepthMessage : IWebSocketMessage
    {
        public string Symbol { get; set; }
        public int UpdateId { get; set; }
        public IEnumerable<OrderBookOffer> Bids { get; set; }
        public IEnumerable<OrderBookOffer> Asks { get; set; }
        public string EventType { get; set; }
        public long EventTime { get; set; }
    }
}