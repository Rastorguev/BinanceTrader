﻿using System.Globalization;
using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Account;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Core;

public class FundsStateLogger
{
    private const string UsdtAsset = "USDT";
    private const string BtcAsset = "BTC";

    [NotNull]
    private readonly IBinanceClient _client;

    [NotNull]
    private readonly ILogger _logger;

    [NotNull]
    private readonly string _quoteAsset;

    public FundsStateLogger(
        [NotNull] IBinanceClient client,
        [NotNull] ILogger logger,
        [NotNull] string quoteAsset)
    {
        _client = client;
        _logger = logger;
        _quoteAsset = quoteAsset;
    }

    public async Task LogFundsState([NotNull] IReadOnlyList<IBalance> funds, [NotNull] IReadOnlyList<string> assets)
    {
        try
        {
            var prices = (await _client.GetAllPrices().NotNull()).ToList();
            var averagePrice = GetAveragePrice(prices, assets);
            var medianPrice = GetMedianPrice(prices, assets);

            var quoteUsdtSymbol = SymbolUtils.GetCurrencySymbol(_quoteAsset, UsdtAsset);
            var btcUsdtSymbol = SymbolUtils.GetCurrencySymbol(_quoteAsset, BtcAsset);

            var quoteTotal = GetFundsTotal(funds, prices);

            var btcTotal = _quoteAsset != BtcAsset
                ? quoteTotal *
                  prices.First(p => p.NotNull().Symbol == btcUsdtSymbol).NotNull().Price
                : quoteTotal;

            var usdtTotal = quoteTotal *
                            prices.First(p => p.NotNull().Symbol == quoteUsdtSymbol).NotNull().Price;

            _logger.LogMessage("Funds", new Dictionary<string, string>
            {
                { "Quote", quoteTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "BTC", btcTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "Usdt", usdtTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "AverageAssetPrice", averagePrice.Round8().ToString(CultureInfo.InvariantCulture) },
                { "MedianAssetPrice", medianPrice.Round8().ToString(CultureInfo.InvariantCulture) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private decimal GetFundsTotal(
        [NotNull] IReadOnlyList<IBalance> funds,
        [NotNull] IReadOnlyList<SymbolPrice> prices)
    {
        var total = 0m;
        foreach (var balance in funds)
        {
            var assetTotal = balance.NotNull().Free + balance.NotNull().Locked;
            if (balance.NotNull().Asset == _quoteAsset)
            {
                total += assetTotal;
            }
            else
            {
                var symbol = $"{balance.NotNull().Asset}{_quoteAsset}";
                var symbolPrice = prices.FirstOrDefault(p => p.NotNull().Symbol == symbol);

                if (symbolPrice == null)
                {
                    continue;
                }

                total += assetTotal * symbolPrice.Price;
            }
        }

        return total;
    }

    private decimal GetAveragePrice(
        [NotNull] IEnumerable<SymbolPrice> prices,
        [NotNull] IEnumerable<string> assets)
    {
        var assetsPrices = GetPrices(prices, assets);

        return assetsPrices.Average().Round8();
    }

    private decimal GetMedianPrice(
        [NotNull] IEnumerable<SymbolPrice> prices,
        [NotNull] IEnumerable<string> assets)
    {
        var assetsPrices = GetPrices(prices, assets);

        return assetsPrices.Median().Round8();
    }

    [NotNull]
    private IEnumerable<decimal> GetPrices([NotNull] IEnumerable<SymbolPrice> prices,
        [NotNull] IEnumerable<string> assets)
    {
        var symbols = assets.Select(a => SymbolUtils.GetCurrencySymbol(a, _quoteAsset));
        var assetsPrices =
            prices.Where(p => symbols.Contains(p.NotNull().Symbol)).Select(p => p.Price).ToList();

        return assetsPrices;
    }
}