using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class TradingRulesProvider
    {
        [NotNull] private readonly IBinanceClient _client;
        private DateTime? _lastUpdateTime;
        private readonly TimeSpan _expiration = TimeSpan.FromHours(12);
        private TradingRules _tradingRules;

        public TradingRulesProvider([NotNull] IBinanceClient client)
        {
            _client = client;
        }

        public async Task UpdateRulesIfNeeded()
        {
            try
            {
                var isValid = _tradingRules != null &&
                              _lastUpdateTime != null &&
                              _lastUpdateTime.Value + _expiration > DateTime.Now;

                if (!isValid)
                {
                    _tradingRules = await _client.LoadTradingRules().NotNull();
                    _lastUpdateTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to load trading rules", ex);
            }
        }

        [NotNull]
        public TradingRule GetRulesFor(string symbol)
        {
            try
            {
                return _tradingRules.NotNull().Symbols.NotNull().First(s => s.NotNull().Symbol == symbol).NotNull();
            }
            catch (Exception ex)
            {
                throw new AppException($"Trading rules for {symbol} are not found", ex);
            }
        }
    }
}