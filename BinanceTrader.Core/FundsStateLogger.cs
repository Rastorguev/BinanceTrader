using System.Globalization;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Extensions;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader;

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
            var prices = (await _client.GetAllPrices().NotNull().NotNull()).ToList();
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
                { "Quote", quoteTotal.Round().ToString(CultureInfo.InvariantCulture) },
                { "BTC", btcTotal.Round().ToString(CultureInfo.InvariantCulture) },
                { "Usdt", usdtTotal.Round().ToString(CultureInfo.InvariantCulture) },
                { "AverageAssetPrice", averagePrice.Round().ToString(CultureInfo.InvariantCulture) },
                { "MedianAssetPrice", medianPrice.Round().ToString(CultureInfo.InvariantCulture) }
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

        return assetsPrices.Average().Round();
    }

    private decimal GetMedianPrice(
        [NotNull] IEnumerable<SymbolPrice> prices,
        [NotNull] IEnumerable<string> assets)
    {
        var assetsPrices = GetPrices(prices, assets);

        return assetsPrices.Median().Round();
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