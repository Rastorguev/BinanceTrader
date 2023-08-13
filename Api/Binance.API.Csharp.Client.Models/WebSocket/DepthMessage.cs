using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;

namespace Binance.API.Csharp.Client.Models.WebSocket
{
    public class DepthMessage : IWebSocketMessage
    {
        public string EventType { get; set; }
        public long EventTime { get; set; }
        public string Symbol { get; set; }
        public int UpdateId { get; set; }
        public IEnumerable<OrderBookOffer> Bids { get; set; }
        public IEnumerable<OrderBookOffer> Asks { get; set; }
    }
}