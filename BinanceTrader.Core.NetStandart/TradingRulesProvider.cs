﻿using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader;

public class TradingRulesProvider
{
    [NotNull]
    private readonly IBinanceClient _client;

    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10);
    private DateTime? _lastUpdateTime;
    private TradingRulesContainer _rulesContainer;

    public TradingRulesProvider([NotNull] IBinanceClient client)
    {
        _client = client;
    }

    public IReadOnlyList<ITradingRules> Rules => _rulesContainer?.Rules.NotNull().ToList() ?? new List<ITradingRules>();

    public async Task UpdateRulesIfNeeded()
    {
        try
        {
            var isValid = _rulesContainer != null &&
                          _lastUpdateTime != null &&
                          _lastUpdateTime.Value + _expiration > DateTime.Now;

            if (!isValid)
            {
                _rulesContainer = await _client.LoadTradingRules().NotNull();
                _lastUpdateTime = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            throw new AppException("Unable to load trading rules", ex);
        }
    }

    [NotNull]
    public ITradingRules GetRulesFor(string symbol)
    {
        try
        {
            return _rulesContainer.NotNull().Rules.NotNull().First(s => s.NotNull().Symbol == symbol).NotNull();
        }
        catch (Exception ex)
        {
            throw new AppException($"Trading rules for {symbol} are not found", ex);
        }
    }

    [NotNull]
    public List<string> GetBaseAssetsFor(string asset)
    {
        return _rulesContainer.NotNull().Rules.NotNull().Where(r => r.NotNull().QuoteAsset == asset)
            .Select(r => r.BaseAsset).ToList();
    }
}