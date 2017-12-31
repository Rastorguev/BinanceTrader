using System;
using System.Collections.Generic;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.Entities.Enums;
using BinanceTrader.Utils;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly BinanceApi _api;
        private decimal _lastOrderPrice;
        private readonly decimal _fee;
        private readonly decimal _fluctuation;
        private readonly Loger _loger;

        private decimal _initialTotalAmount;
        private TraderState _state = TraderState.Idle;
        private DateTime _lastUpdateTime;
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromMinutes(2);
        private readonly Dictionary<TraderState, Func<TraderState>> _stateActionMap =
            new Dictionary<TraderState, Func<TraderState>>();

        private decimal _currentPrice;
        private readonly int _stopLossPercents;
        private readonly string _currencySymbol;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;
        private Balances _currentBalances;

        public Trader(
            BinanceApi api,
            string baseAsset,
            string quoteAsset,
            decimal fluctuationPercents = 0.1m,
            decimal fee = 0.05m,
            int stopLossPercents = 2)
        {
            _api = api;

            _baseAsset = baseAsset;
            _quoteAsset = quoteAsset;
            _currencySymbol = ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset);
            _fee = fee;
            _fluctuation = fluctuationPercents;
            _loger = new Loger(baseAsset, quoteAsset);
            _stopLossPercents = stopLossPercents;

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

        public void Trade(
        )
        {
            State = TraderState.Initialization;

            Init();

            State = TraderState.InitialBuy;

            while (true)
            {
                _currentPrice = _api.GetPrices().Result.PriceFor(_currencySymbol).Price;

                State = _stateActionMap[State].Invoke();
            }
        }

        private void Init()
        {
            _currentBalances = GetBalances();
            var price = _api.GetPrices().Result.PriceFor(_currencySymbol).Price;
            _initialTotalAmount = GetTotalAmount(_currentBalances, price);
        }

        private TraderState InitialBuyAction()
        {
            if (Buy())
            {
                return TraderState.Sell;
            }

            return TraderState.InitialBuy;
        }

        private TraderState BuyAction()
        {
            if (_currentPrice + _lastOrderPrice.Percents(_fluctuation) < _lastOrderPrice)
            {
                if (Buy())
                {
                    return TraderState.Sell;
                }
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
                if (Sell())
                {
                    return TraderState.Buy;
                }
            }

            if (DateTime.Now - _lastUpdateTime > _maxIdlePeriod)
            {
                return TraderState.ForceSell;
            }

            return TraderState.Sell;
        }

        private TraderState ForceBuyAction()
        {
            var quoteAmount = _currentBalances.GetBalanceFor(_quoteAsset).Free;
            var baseAmount = quoteAmount / _currentPrice;
            var profit = CalculateProfit(_initialTotalAmount, _currentPrice, baseAmount, _fee);
            if (profit > -_stopLossPercents)
            {
                if (Buy())
                {
                    return TraderState.Sell;
                }
            }

            return TraderState.ForceBuy;
        }

        private TraderState ForceSellAction()
        {
            var baseAmount = _currentBalances.GetBalanceFor(_baseAsset).Free;
            var profit = CalculateProfit(_initialTotalAmount, _currentPrice, baseAmount, _fee);
            if (profit > -_stopLossPercents)
            {
                if (Sell())
                {
                    return TraderState.Buy;
                }
            }

            return TraderState.ForceSell;
        }

        private bool Buy()
        {
            var succeed = HandleOrderRequest(OrderSide.Buy, _currentPrice, GetAmountToBuy());

            return succeed;
        }

        private bool Sell()
        {
            var succeed = HandleOrderRequest(OrderSide.Sell, _currentPrice, GetAmountToSell());

            return succeed;
        }

        private bool HandleOrderRequest(OrderSide side, decimal price, decimal amount)
        {
            var succeed = MakeOrder(side, price, amount);
            if (succeed)
            {
                _lastOrderPrice = _currentPrice;
                _currentBalances = GetBalances();
            }

            Log(_currentBalances, GetTotalAmount(_currentBalances, _currentPrice), succeed);

            return succeed;
        }

        private bool MakeOrder(OrderSide side, decimal price, decimal amount)
        {
            var orderConfig = new OrderConfig
            {
                BaseCurrency = _baseAsset,
                QuoteCurrency = _quoteAsset,
                Price = price,
                Quantity = amount,
                TimeInForce = TimeInForceType.IOC,
                Side = side,
                Type = OrderType.Limit
            };

            var result = _api.CreateOrder(orderConfig).Result;

            return result.Status == OrderStatus.Filled;
        }

        private void Log(Balances balances, decimal totalAmount, bool succed)
        {
            var baseAmount = balances.GetBalanceFor(_baseAsset).Free;
            var quoteAmount = balances.GetBalanceFor(_quoteAsset).Free;

            _loger.Log(
                State,
                succed,
                _currentPrice,
                baseAmount,
                quoteAmount,
                totalAmount,
                CalculateProfit(_initialTotalAmount, GetTotalAmount(_currentBalances, _currentPrice)));
        }

        private static decimal CalculateProfit(decimal initialAmount, decimal currentAmount)
        {
            var profit = (currentAmount - initialAmount) * 100 / initialAmount;

            return profit;
        }

        private static decimal CalculateProfit(decimal initialAmount, decimal price, decimal baseAmount, decimal fee)
        {
            var qouteAmount = price * baseAmount;
            var netAmount = qouteAmount + qouteAmount.Percents(fee);

            return CalculateProfit(initialAmount, netAmount);
        }

        private decimal GetTotalAmount(Balances balances, decimal price)
        {
            var baseAmount = balances.GetBalanceFor(_baseAsset);
            var quoteAmount = balances.GetBalanceFor(_quoteAsset);

            return quoteAmount.Free + baseAmount.Free * price;
        }

        private Balances GetBalances()
        {
            return _api.GetAccountInfo().Result.Balances;
        }

        private decimal GetAmountToBuy()
        {
            var quoteAmount = _currentBalances.GetBalanceFor(_quoteAsset).Free;
            var baseAmount = Math.Floor(quoteAmount / _currentPrice);
            return baseAmount;
        }

        private decimal GetAmountToSell()
        {
            var baseAmount = Math.Floor(_currentBalances.GetBalanceFor(_baseAsset).Free);
            return baseAmount;
        }
    }
}