using System;
using System.Collections.Generic;
using System.Globalization;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using BinanceTrader.Trader;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;

namespace BinanceTrader.WebJob
{
    public class Logger : ILogger
    {
        [NotNull] private readonly TelemetryClient _client;

        public Logger()
        {
            const string key = "792fccae-78e5-414f-8bb3-804ec0f6a4d1";
            _client = new TelemetryClient {InstrumentationKey = key};
        }

        public void LogOrder(string orderEvent, IOrder order)
        {
            _client.TrackEvent(orderEvent, new Dictionary<string, string>
            {
                {"Symbol", order.Symbol},
                {"Side", order.Side.ToString()},
                {"Status", order.Status.ToString()},
                {"Price", order.Price.Round().ToString(CultureInfo.InvariantCulture)},
                {"Qty", order.OrigQty.Round().ToString(CultureInfo.InvariantCulture)}
            });
        }

        public void LogMessage(string key, string message)
        {
            _client.TrackEvent(key, new Dictionary<string, string>
            {
                {key, message}
            });
        }

        public void LogWarning(string key, string message)
        {
            _client.TrackEvent(key, new Dictionary<string, string>
            {
                {key, message}
            });
        }

        public void LogException(Exception ex)
        {
            _client.TrackException(ex);
        }
    }
}