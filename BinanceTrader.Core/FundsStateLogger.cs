using System.Globalization;
using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Account;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;

namespace BinanceTrader.Core;

public class FundsStateLogger
{
    private const string UsdtAsset = "USDT";
    private const string BtcAsset = "BTC";

    private readonly IBinanceClient _client;
    private readonly ILogger _logger;
    private readonly string _quoteAsset;
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(5);
    private DateTime? _lastLogTime;

    public FundsStateLogger(
        IBinanceClient client,
        ILogger logger,
        string quoteAsset)
    {
        _client = client;
        _logger = logger;
        _quoteAsset = quoteAsset;
    }

    public async Task LogFundsStateIfNeeded(IReadOnlyList<IBalance> funds, IReadOnlyList<string> assets)
    {
        var needToLog = _lastLogTime != null &&
                        _lastLogTime.Value + _expiration > DateTime.Now;

        if (!needToLog)
        {
            return;
        }

        try
        {
            var prices = (await _client.GetAllPrices()).ToList();
            var averagePrice = GetAveragePrice(prices, assets);
            var medianPrice = GetMedianPrice(prices, assets);

            var quoteUsdtSymbol = SymbolUtils.GetCurrencySymbol(_quoteAsset, UsdtAsset);
            var btcUsdtSymbol = SymbolUtils.GetCurrencySymbol(_quoteAsset, BtcAsset);

            var quoteTotal = GetFundsTotal(funds, prices);

            var btcTotal = _quoteAsset != BtcAsset
                ? quoteTotal *
                  prices.First(p => p.Symbol == btcUsdtSymbol).Price
                : quoteTotal;

            var usdtTotal = quoteTotal *
                            prices.First(p => p.Symbol == quoteUsdtSymbol).Price;

            _logger.LogMessage("Funds", new Dictionary<string, string>
            {
                { "Quote", quoteTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "BTC", btcTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "Usdt", usdtTotal.Round8().ToString(CultureInfo.InvariantCulture) },
                { "AverageAssetPrice", averagePrice.Round8().ToString(CultureInfo.InvariantCulture) },
                { "MedianAssetPrice", medianPrice.Round8().ToString(CultureInfo.InvariantCulture) }
            });

            _lastLogTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private decimal GetFundsTotal(
        IReadOnlyList<IBalance> funds,
        IReadOnlyList<SymbolPrice> prices)
    {
        var total = 0m;
        foreach (var balance in funds)
        {
            var assetTotal = balance.Free + balance.Locked;
            if (balance.Asset == _quoteAsset)
            {
                total += assetTotal;
            }
            else
            {
                var symbol = $"{balance.Asset}{_quoteAsset}";
                var symbolPrice = prices.FirstOrDefault(p => p.Symbol == symbol);

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
        IEnumerable<SymbolPrice> prices,
        IEnumerable<string> assets)
    {
        var assetsPrices = GetPrices(prices, assets);

        return assetsPrices.Average().Round8();
    }

    private decimal GetMedianPrice(
        IEnumerable<SymbolPrice> prices,
        IEnumerable<string> assets)
    {
        var assetsPrices = GetPrices(prices, assets);

        return assetsPrices.Median().Round8();
    }

    private IEnumerable<decimal> GetPrices(IEnumerable<SymbolPrice> prices,
        IEnumerable<string> assets)
    {
        var symbols = assets.Select(a => SymbolUtils.GetCurrencySymbol(a, _quoteAsset));
        var assetsPrices =
            prices.Where(p => symbols.Contains(p.Symbol)).Select(p => p.Price).ToList();

        return assetsPrices;
    }
}