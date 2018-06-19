using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class FundsStateChecker
    {
        private const string UsdtAsset = "USDT";

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly string _quoteAsset;

        public FundsStateChecker(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger,
            [NotNull] string quoteAsset)
        {
            _client = client;
            _logger = logger;
            _quoteAsset = quoteAsset;
        }

        [NotNull]
        public IReadOnlyList<string> Assets { get; set; } = new List<string>();

        public async Task LogFundsState()
        {
            try
            {
                var prices = (await _client.GetAllPrices().NotNull().NotNull()).ToList();
                var assetsAveragePrice = GetTradingAssetsAveragePrice(prices);

                var funds = (await _client.GetAccountInfo().NotNull()).NotNull()
                    .Balances.NotNull().Where(b => b.NotNull().Free + b.NotNull().Locked > 0).ToList();

                var quoteUsdtSymbol = SymbolUtils.GetCurrencySymbol(_quoteAsset, UsdtAsset);
                var quoteTotal = GetFundsTotal(funds, prices);
                var usdtTotal = quoteTotal *
                                prices.First(p => p.NotNull().Symbol == quoteUsdtSymbol).NotNull().Price;

                _logger.LogMessage("Funds", new Dictionary<string, string>
                {
                    {"Quote", quoteTotal.Round().ToString(CultureInfo.InvariantCulture)},
                    {"Usdt", usdtTotal.Round().ToString(CultureInfo.InvariantCulture)},
                    {"AverageAssetPrice", assetsAveragePrice.Round().ToString(CultureInfo.InvariantCulture)}
                });
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private decimal GetFundsTotal(
            [NotNull] IReadOnlyList<Balance> funds,
            [NotNull] IReadOnlyList<SymbolPrice> prices)
        {
            var total = 0m;
            foreach (var fund in funds)
            {
                var assetTotal = fund.NotNull().Free + fund.NotNull().Locked;
                if (fund.NotNull().Asset == _quoteAsset)
                {
                    total += assetTotal;
                }
                else
                {
                    var symbol = $"{fund.NotNull().Asset}{_quoteAsset}";
                    total += assetTotal * prices.First(p => p.NotNull().Symbol == symbol).NotNull().Price;
                }
            }

            return total;
        }

        private decimal GetTradingAssetsAveragePrice([NotNull] IReadOnlyList<SymbolPrice> prices)
        {
            var symbols = Assets.Select(a => SymbolUtils.GetCurrencySymbol(a, _quoteAsset));
            var tradingAssetsPrices =
                prices.Where(p => symbols.Contains(p.NotNull().Symbol)).Select(p => p.Price).ToList();
            return tradingAssetsPrices.Average().Round();
        }
    }
}