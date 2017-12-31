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
        private decimal _fluctuation;
        private Loger _loger;

        private decimal _initialQuoteAmount;
        private TraderState _state;
        private DateTime _lastUpdateTime;
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromSeconds(1);
        private readonly Dictionary<TraderState, Func<TraderState>> _stateActionMap =
            new Dictionary<TraderState, Func<TraderState>>();

        private decimal _currentPrice;
        private int _stopLossPercents;

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

                _state = value;
                _lastUpdateTime = DateTime.Now;
            }
        }

        public async void Trade(
            string baseCurrency,
            string quoteCurrency,
            decimal quoteAmount,
            decimal fluctuation = 0.2m,
            decimal fee = 0.05m,
            int stopLossPercents = 10)
        {
            _quoteAmount = _initialQuoteAmount = quoteAmount;
            _fee = fee;
            _fluctuation = fluctuation;
            _loger = new Loger(baseCurrency, quoteCurrency);
            _stopLossPercents = stopLossPercents;

            State = TraderState.InitialBuy;

            while (true)
            {
                var currencyPair = ApiUtils.CreateCurrencySymbol(baseCurrency, quoteCurrency);
                _currentPrice = (await GetCurrencyPrice(currencyPair)).Price;

                State = _stateActionMap[State].Invoke();
            }
        }

        private TraderState InitialBuyAction()
        {
            Buy();

            return TraderState.Sell;
        }

        private TraderState BuyAction()
        {
            if (_currentPrice + _lastOrderPrice.Percents(_fluctuation) < _lastOrderPrice)
            {
                Buy();

                return TraderState.Sell;
            }

            if (DateTime.Now - _lastUpdateTime > _maxIdlePeriod)
            {
                return TraderState.ForceBuy;
            }

            return TraderState.Buy;
        }

        private TraderState SellAction()
        {
            if (_currentPrice > _lastOrderPrice + _lastOrderPrice.Percents(_fluctuation))
            {
                Sell();

                return TraderState.Buy;
            }

            if (DateTime.Now - _lastUpdateTime > _maxIdlePeriod)
            {
                return TraderState.ForceSell;
            }

            return TraderState.Sell;
        }

        private TraderState ForceBuyAction()
        {
            var profit = CalculateProfit(_currentPrice);
            if (profit > -_stopLossPercents)
            {
                Buy();

                return TraderState.Sell;
            }

            return TraderState.ForceBuy;
        }

        private TraderState ForceSellAction()
        {
            var profit = CalculateProfit(_currentPrice);
            if (profit > -_stopLossPercents)
            {
                Sell();

                return TraderState.Buy;
            }

            return TraderState.ForceSell;
        }

        private void Sell()
        {
            _quoteAmount = SubstractFee(_baseAmount * _currentPrice, _fee);
            _baseAmount = 0;
            _lastOrderPrice = _currentPrice;

            Log();
        }

        private void Buy()
        {
            _baseAmount = SubstractFee(_quoteAmount / _currentPrice, _fee);
            _quoteAmount = 0;
            _lastOrderPrice = _currentPrice;

            Log();
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

        private void Log()
        {
            _loger.Log(State, _currentPrice, _baseAmount, _quoteAmount, CalculateProfit(_currentPrice));
        }

        private decimal CalculateProfit(decimal price)
        {
            var quoteAmount = _quoteAmount > 0 ? _quoteAmount : _baseAmount * price;
            var profit = (quoteAmount - _initialQuoteAmount) * 100 / _initialQuoteAmount;

            return profit;
        }

        //private decimal CalculateQuoteAmount(decimal quoteAmount,  )
        //{
        //    var quoteAmount = _quoteAmount > 0 ? _quoteAmount : _baseAmount * price;
        //}

        //private decimal CalculateProfit(decimal quoteAmount, decimal initialQuoteAmount)
        //{
        //    var profit = (quoteAmount - initialQuoteAmount) * 100 / initialQuoteAmount;

        //    return profit;
        //}
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