using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class TradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;
        [NotNull ]private MockTradeAccount _account;
        private decimal _nextPrice;
        private OrderSide _nextAction;
        private DateTime? _lastActionDate;

        public TradeSession(
            [NotNull] TradeSessionConfig config)
        {
            _config = config;
        }

        [NotNull]
        public ITradeAccount Run([NotNull] [ItemNotNull] IReadOnlyList<Candlestick> candles)
        {
            _account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);

            if (!candles.Any())
            {
                return _account;
            }

            _nextPrice = candles.First().Close;
            _nextAction = OrderSide.Buy;
            _lastActionDate = null;

            foreach (var candle in candles)
            {
                var force = _lastActionDate == null ||
                            candle.CloseTime.GetTime() - _lastActionDate.Value >=
                            TimeSpan.FromHours((double)_config.MaxIdleHours);

                var profitRatio = _config.MinProfitRatio;
                var inRange = _nextPrice >= candle.Low && _nextPrice <= candle.High;

                var n = 10m;
                if (_nextAction == OrderSide.Buy)
                {
             
                    if (inRange)
                    {
                        var price = _nextPrice;
                        Buy(price, candle, profitRatio);
                    }
                    //else if (candle.Low - _nextPrice > _nextPrice.Percents(n))
                    //{
                    //    var price = candle.Low;
                    //    Buy(price, candle, profitRatio);
                    //}
                    else if (force)
                    {
                        var price = candle.High;
                        Buy(price, candle, profitRatio);
                    }
                }
                else if (_nextAction == OrderSide.Sell)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;
                        Sell(price, candle, profitRatio);
                    }
                    //else if (_nextPrice - candle.High > _nextPrice.Percents(n))
                    //{
                    //    var price = candle.High;
                    //    Sell(price, candle, profitRatio);
                    //}
                    else if (force)
                    {
                        var price = candle.Low;
                        Sell(price, candle, profitRatio);
                    }
                }
            }

            return _account;
        }

        private void Buy(decimal price, Candlestick candle, decimal profitRatio)
        {
            var estimatedBaseAmount = Math.Floor(_account.CurrentQuoteAmount / price);
            if (_account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
            {
                _account.Buy(estimatedBaseAmount, price, candle.OpenTime.GetTime());

                _nextPrice = price + price.Percents(profitRatio);
                _nextAction = OrderSide.Sell;
                _lastActionDate = candle.OpenTime.GetTime();
            }
        }

        private void Sell(decimal price, Candlestick candle, decimal profitRatio)
        {
            var baseAmount = Math.Floor(_account.CurrentBaseAmount);
            if (baseAmount > 0)
            {
                _account.Sell(baseAmount, price, candle.OpenTime.GetTime());

                _nextPrice = price - price.Percents(profitRatio);
                _nextAction = OrderSide.Buy;
                _lastActionDate = candle.OpenTime.GetTime();
            }
        }

        private static void Log(
            OrderSide nextAction,
            Candlestick candle,
            decimal price,
            ITradeAccount account,
            bool force)
        {
            Console.WriteLine(nextAction);
            Console.WriteLine(candle.OpenTime);
            Console.WriteLine(price.Round());
            Console.WriteLine(account.GetProfit());

            if (force)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine(force);

            Console.ResetColor();

            Console.WriteLine();
        }
    }
}