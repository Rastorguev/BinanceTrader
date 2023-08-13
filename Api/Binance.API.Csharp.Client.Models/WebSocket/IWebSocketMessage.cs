namespace Binance.API.Csharp.Client.Models.WebSocket
{
    public interface IWebSocketMessage
    {
        long EventTime { get; set; }
        string EventType { get; set; }
    }
}