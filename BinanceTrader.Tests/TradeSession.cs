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

        public TradeSession(
            [NotNull] TradeSessionConfig config)
        {
            _config = config;
        }

        [NotNull]
        public ITradeAccount Run([NotNull] [ItemNotNull] List<Candlestick> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            var nextPrice = candles.First().Close;
            var nextAction = OrderSide.Buy;
            DateTime? lastActionDate = null;

            foreach (var candle in candles)
            {
                var force = lastActionDate == null ||
                            candle.CloseTime.GetTime() - lastActionDate.Value >= TimeSpan.FromHours(_config.MaxIdleHours);

                //var force = false;
                var minProfitRatio = _config.MinProfitRatio;
                var inRange = nextPrice >= candle.Low && nextPrice <= candle.High;

                if (nextAction == OrderSide.Buy && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.High;
                    }

                    var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
                    {
                        account.Buy(estimatedBaseAmount, price, candle.OpenTime.GetTime());

                        //Log(nextAction, candle, price, account, force);

                        nextPrice = price + price.Percents(minProfitRatio);
                        nextAction = OrderSide.Sell;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
                else if (nextAction == OrderSide.Sell && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.Low;
                    }

                    var baseAmount = Math.Floor(account.CurrentBaseAmount);
                    if (baseAmount > 0)
                    {
                        account.Sell(baseAmount, price, candle.OpenTime.GetTime());

                        //Log(nextAction, candle, price, account, force);

                        nextPrice = price - price.Percents(minProfitRatio);
                        nextAction = OrderSide.Buy;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
            }

            return account;
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