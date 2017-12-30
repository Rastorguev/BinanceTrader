using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.Utils;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly BinanceApi _binanceApi;
        private decimal _lastOrderPrice;
        private decimal _baseAmount;
        private decimal _quoteAmount;
        private decimal _fee;
        private decimal _tolerance;
        private Loger _loger;

        private decimal _initialQuoteAmount;
        private TraderState _state;
        private DateTime _lastUpdateTime;
        private readonly TimeSpan _idlePeriod = TimeSpan.FromMinutes(1);
        private readonly Dictionary<TraderState, Func<decimal, TraderState>> _stateActionMap =
            new Dictionary<TraderState, Func<decimal, TraderState>>();

        public Trader(BinanceApi binanceApi)
        {
            _binanceApi = binanceApi;
            _stateActionMap.Add(TraderState.InitialBuy, InitialBuyAction);
            _stateActionMap.Add(TraderState.Buy, BuyAction);
            _stateActionMap.Add(TraderState.Sell, SellAction);
            _stateActionMap.Add(TraderState.ForceBuy, ForceBuyAction);
            _stateActionMap.Add(TraderState.ForceSell, ForceSellAction);
        }

        public TraderState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                {
                    return;
                }
                _lastUpdateTime = DateTime.Now;
                _state = value;
            }
        }

        public async void Trade(
            string baseCurrency,
            string quoteCurrency,
            decimal quoteAmount,
            decimal tolerance = 0.2m,
            decimal fee = 0.05m)
        {
            _quoteAmount = _initialQuoteAmount = quoteAmount;
            _fee = fee;
            _tolerance = tolerance;
            _loger = new Loger(baseCurrency, quoteCurrency);

            State = TraderState.InitialBuy;

            while (true)
            {
                var currencyPair = string.Format($"{baseCurrency}{quoteCurrency}");
                var price = (await GetCurrencyPrice(currencyPair)).Price;

                State = _stateActionMap[State].Invoke(price);
            }
        }

        private TraderState InitialBuyAction(decimal price)
        {
            Buy(price);
            _loger.Log(State, price, _baseAmount, _quoteAmount, CalculateProfit(price));

            return TraderState.Sell;
        }

        private TraderState BuyAction(decimal price)
        {
            if (price + _lastOrderPrice.Percents(_tolerance) < _lastOrderPrice)
            {
                Buy(price);
                _loger.Log(State, price, _baseAmount, _quoteAmount, CalculateProfit(price));

                return TraderState.Sell;
            }

            if (DateTime.Now - _lastUpdateTime > _idlePeriod)
            {
                return TraderState.ForceBuy;
            }

            return TraderState.Buy;
        }

        private TraderState SellAction(decimal price)
        {
            if (price > _lastOrderPrice + _lastOrderPrice.Percents(_tolerance))
            {
                Sell(price);
                _loger.Log(State, price, _baseAmount, _quoteAmount, CalculateProfit(price));

                return TraderState.Buy;
            }

            if (DateTime.Now - _lastUpdateTime > _idlePeriod)
            {
                return TraderState.ForceSell;
            }

            return TraderState.Sell;
        }

        private TraderState ForceBuyAction(decimal price)
        {
            var profit = CalculateProfit(price);
            if (profit > 0)
            {
                Buy(price);
                _loger.Log(State, price, _baseAmount, _quoteAmount, CalculateProfit(price));

                return TraderState.Sell;
            }

            return TraderState.ForceBuy;
        }

        private TraderState ForceSellAction(decimal price)
        {
            var profit = CalculateProfit(price);
            if (profit > 0)
            {
                Sell(price);
                _loger.Log(State, price, _baseAmount, _quoteAmount, CalculateProfit(price));

                return TraderState.Buy;
            }

            return TraderState.ForceSell;
        }

        private void Sell(decimal price)
        {
            _quoteAmount = SubstractFee(_baseAmount * price, _fee);
            _baseAmount = 0;
            _lastOrderPrice = price;
        }

        private void Buy(decimal price)
        {
            _baseAmount = SubstractFee(_quoteAmount / price, _fee);
            _quoteAmount = 0;
            _lastOrderPrice = price;
        }

        private async Task<CurrencyPrice> GetCurrencyPrice(string cyrrencySymbol)
        {
            var prices = await _binanceApi.GetPrices();
            var priceTicker = new PriceTicker(prices);
            var price = priceTicker.GetPrice(cyrrencySymbol);
            return price;
        }

        private static decimal SubstractFee(decimal value, decimal fee)
        {
            return value - value.Percents(fee);
        }

        private decimal CalculateProfit(decimal price)
        {
            var quoteAmount = _quoteAmount > 0 ? _quoteAmount : _baseAmount * price;
            var profit = (quoteAmount - _initialQuoteAmount) * 100 / _initialQuoteAmount;

            return profit;
        }
    }

    public enum TraderState
    {
        InitialBuy,
        Buy,
        Sell,
        ForceBuy,
        ForceSell
    }
}