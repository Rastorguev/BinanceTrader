﻿using System.Collections;
using System.Globalization;
using BinanceApi.Models.WebSocket;
using BinanceTrader.Core;
using BinanceTrader.Tools;
using Microsoft.ApplicationInsights;

namespace BinanceTrader.WebJob;

public class Logger : ILogger
{
    private const string InstrumentationKey = "cf5db4c4-8f2b-444b-94b9-7957f4b476b8";
    private readonly string _traderName;

    private readonly TelemetryClient _client;

    public Logger(string traderName)
    {
#pragma warning disable CS0618
        _client = new TelemetryClient { InstrumentationKey = InstrumentationKey };
#pragma warning restore CS0618
        _traderName = traderName;
    }

    public void LogOrderPlaced(IOrder order)
    {
#if DEBUG
        LogOrder("Placed", order);
#endif
    }

    public void LogOrderCompleted(IOrder order)
    {
        LogOrder("Completed", order);
    }

    public void LogOrderCanceled(IOrder order)
    {
#if DEBUG
        LogOrder("Canceled", order);
#endif
    }

    public void LogOrderRequest(string eventName, OrderRequest orderRequest)
    {
        _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                { "Symbol", orderRequest.Symbol },
                { "Side", orderRequest.Side.ToString() },
                { "Price", orderRequest.Price.Round8().ToString(CultureInfo.InvariantCulture) },
                { "Qty", orderRequest.Qty.Round8().ToString(CultureInfo.InvariantCulture) },
                { "Total", (orderRequest.Qty * orderRequest.Price).Round8().ToString(CultureInfo.InvariantCulture) }
            }
            .AddTraderName(_traderName));

        _client.Flush();
    }

    public void LogMessage(string eventName, string message)
    {
        _client.TrackEvent(eventName, new Dictionary<string, string>
            {
                { eventName, message }
            }
            .AddTraderName(_traderName));

        _client.Flush();
    }

    public void LogMessage(string eventName, Dictionary<string, string> properties)
    {
        _client.TrackEvent(eventName, properties.AddTraderName(_traderName));
    }

    public void LogException(Exception ex)
    {
        var properties = new Dictionary<string, string>();
        foreach (DictionaryEntry d in ex.Data)
        {
            properties.Add(d.Key.ToString()!, d.Value!.ToString()!);
        }

        properties.AddTraderName(_traderName);
        _client.TrackException(ex, properties);
        _client.Flush();
    }

    public void LogWarning(string eventName, string message)
    {
        _client.TrackEvent(eventName, new Dictionary<string, string>
        {
            { eventName, message }
        }.AddTraderName(_traderName));

        _client.Flush();
    }

    private void LogOrder(string eventName, IOrder order)
    {
        _client.TrackEvent(eventName, new Dictionary<string, string>
        {
            { "Symbol", order.Symbol },
            { "Side", order.Side.ToString() },
            { "Status", order.Status.ToString() },
            { "Price", order.Price.Round8().ToString(CultureInfo.InvariantCulture) },
            { "Qty", order.OrderQuantity.Round8().ToString(CultureInfo.InvariantCulture) },
            { "Total", (order.OrderQuantity * order.Price).Round8().ToString(CultureInfo.InvariantCulture) }
        }.AddTraderName(_traderName));

        _client.Flush();
    }
}

public static class LoggerHelpers
{
    public static Dictionary<string, string> AddTraderName(this Dictionary<string, string> properties,
        string traderName)
    {
        properties.Add("Trader", traderName);
        return properties;
    }
}