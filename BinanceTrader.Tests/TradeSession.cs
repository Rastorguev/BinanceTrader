using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class TradeSession
{
    private readonly TradeSessionConfig _config;
    private decimal _expectedPrice;
    private OrderSide _nextAction;
    private DateTime? _lastActionTime;
    private ITradeAccount _account;
    private readonly decimal _profitRatio;

    public TradeSession(TradeSessionConfig config)
    {
        _config = config;
        _profitRatio = _config.ProfitRatio.Round8();
    }

    public ITradeAccount Run(IReadOnlyList<Candlestick> candles)
    {
        _account = new MockTradeAccount(
            0,
            _config.InitialQuoteAmount,
            _config.FeePercent,
            _config.FeeAssetToQuoteConversionRatio);

        if (!candles.Any())
        {
            return _account;
        }

        _expectedPrice = candles[0].Close.Round8();
        _nextAction = OrderSide.Buy;
        _lastActionTime = null;

        foreach (var candle in candles)
        {
            var isExpired = _lastActionTime == null ||
                            candle.CloseTime.GetTime() - _lastActionTime.Value >= _config.MaxIdlePeriod;

            var isInRange = _expectedPrice > candle.Low.Round8() && _expectedPrice < candle.High.Round8();
            var time = candle.OpenTime.GetTime();

            switch (_nextAction)
            {
                case OrderSide.Buy when isInRange:
                {
                    var price = _expectedPrice;
                    var expectedPrice = (price + price.Percents(_profitRatio)).Round8();

                    Buy(price, expectedPrice, time);
                    break;
                }
                case OrderSide.Buy when isExpired:
                {
                    var price = candle.High.Round8();
                    var expectedPrice = (price - price.Percents(_profitRatio)).Round8();

                    Cancel(expectedPrice, time);
                    break;
                }
                case OrderSide.Sell when isInRange:
                {
                    var price = _expectedPrice;
                    var expectedPrice = (price - price.Percents(_profitRatio)).Round8();

                    Sell(price, expectedPrice, time);
                    break;
                }
                case OrderSide.Sell when isExpired:
                {
                    var price = candle.Low.Round8();
                    var expectedPrice = price + price.Percents(_profitRatio).Round8();

                    Cancel(expectedPrice, time);
                    break;
                }
            }
        }

        return _account;
    }

    private void Buy(decimal price, decimal expectedPrice, DateTime time)
    {
        var estimatedBaseAmount = Math.Floor(_account.CurrentQuoteAmount / price);
        if (_account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
        {
            _expectedPrice = expectedPrice;
            _nextAction = OrderSide.Sell;
            _lastActionTime = time;

            _account.Buy(estimatedBaseAmount, price, time);
        }
    }

    private void Sell(decimal price, decimal expectedPrice, DateTime time)
    {
        var baseAmount = Math.Floor(_account.CurrentBaseAmount);
        if (baseAmount > 0)
        {
            _expectedPrice = expectedPrice;
            _nextAction = OrderSide.Buy;
            _lastActionTime = time;

            _account.Sell(baseAmount, price, time);
        }
    }

    private void Cancel(decimal expectedPrice, DateTime time)
    {
        _expectedPrice = expectedPrice;
        _lastActionTime = time;

        _account.Cancel();
    }
}