using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;
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
        public ITradeAccount Run([NotNull] [ItemNotNull] List<Candle> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            var nextPrice = candles.First().ClosePrice;
            var nextAction = TradeActionType.Buy;
            DateTime? lastActionDate = null;

            foreach (var candle in candles)
            {
                var force = lastActionDate == null ||
                            candle.CloseTime - lastActionDate.Value >= TimeSpan.FromHours(_config.MaxIdleHours);

                //var force = false;
                var minProfitRatio = _config.MinProfitRatio;
                var inRange = nextPrice >= candle.LowPrice && nextPrice <= candle.HighPrice;

                if (nextAction == TradeActionType.Buy && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.HighPrice;
                    }

                    var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
                    {
                        account.Buy(estimatedBaseAmount, price, candle.OpenTime);

                        //Log(nextAction, candle, price, account, force);

                        nextPrice = price + price.Percents(minProfitRatio);
                        nextAction = TradeActionType.Sell;
                        lastActionDate = candle.OpenTime;
                    }
                }
                else if (nextAction == TradeActionType.Sell && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.LowPrice;
                    }

                    var baseAmount = Math.Floor(account.CurrentBaseAmount);
                    if (baseAmount > 0)
                    {
                        account.Sell(baseAmount, price, candle.OpenTime);

                        //Log(nextAction, candle, price, account, force);

                        nextPrice = price - price.Percents(minProfitRatio);
                        nextAction = TradeActionType.Buy;
                        lastActionDate = candle.OpenTime;
                    }
                }
            }

            return account;
        }

        private static void Log(
            TradeActionType nextAction,
            Candle candle,
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