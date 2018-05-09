using System;
using System.Collections;
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
            const string key = "5d67a61a-6cc1-4a03-9989-5b838583d5a1";
            _client = new TelemetryClient {InstrumentationKey = key};
        }

        public void LogOrder(string eventName, IOrder order)
        {
            _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                {"Symbol", order.Symbol},
                {"Side", order.Side.ToString()},
                {"Status", order.Status.ToString()},
                {"Price", order.Price.Round().ToString(CultureInfo.InvariantCulture)},
                {"Qty", order.OrigQty.Round().ToString(CultureInfo.InvariantCulture)},
                {"Total", (order.OrigQty * order.Price).Round().ToString(CultureInfo.InvariantCulture)}
            });

            _client.Flush();
        }

        public void LogOrderRequest(string eventName, OrderRequest orderRequest)
        {
            _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                {"Symbol", orderRequest.Symbol},
                {"Side", orderRequest.Side.ToString()},
                {"Price", orderRequest.Price.Round().ToString(CultureInfo.InvariantCulture)},
                {"Qty", orderRequest.Qty.Round().ToString(CultureInfo.InvariantCulture)},
                {"Total", (orderRequest.Qty * orderRequest.Price).Round().ToString(CultureInfo.InvariantCulture)}
            });

            _client.Flush();
        }

        public void LogMessage(string eventName, string message)
        {
            _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                {eventName, message}
            });
            _client.Flush();
        }

        public void LogMessage(string eventName, Dictionary<string, string> properties)
        {
            _client.TrackEvent(eventName, properties);
        }

        public void LogWarning(string eventName, string message)
        {
            _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                {eventName, message}
            });

            _client.Flush();
        }

        public void LogException(Exception ex)
        {
            var properties = new Dictionary<string, string>();
            foreach (DictionaryEntry d in ex.Data)
            {
                properties.Add(d.Key.ToString(), d.Value.ToString());
            }

            _client.TrackException(ex, properties);
            _client.Flush();
        }
    }
}