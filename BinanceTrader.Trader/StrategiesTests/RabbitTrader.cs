using System;
using BinanceTrader.Api;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;
using BinanceTrader.Tools;


namespace BinanceTrader
{
    public class RabbitTrader
    {
        private readonly BinanceApi _api;
        private readonly string _baseAsset;
        private readonly string _quoteAsset;
        private readonly decimal _fee;

        private readonly decimal _oneDealProfit;
        private readonly decimal _minOrderAmount;
        private readonly Loger _loger;

        private readonly decimal _fluctuation;
        private decimal _initialTotalAmount;
        private readonly string _currencySymbol;
        private Order _lastOrder;
        private DateTime _lastOrderTime;

        public RabbitTrader(
            BinanceApi api,
            string baseAsset,
            string quoteAsset,
            decimal oneDealProfit = 0.1m,
            decimal fee = 0.1m,
            decimal minOrderAmount = 0.01m)
        {
            _api = api;
            _baseAsset = baseAsset;
            _quoteAsset = quoteAsset;
            _currencySymbol = ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset);
            _fee = fee;
            _oneDealProfit = oneDealProfit;
            _minOrderAmount = minOrderAmount;
            _loger = new Loger(baseAsset, quoteAsset);
            _fluctuation = _fee + _oneDealProfit;
        }

        public void Start()
        {
            Init();

            while (true)
            {
                Trade();
            }
        }

        private void Init()
        {
            var balances = GetBalances();
            var prices = _api.GetPrices().Result;
            var price = prices.PriceFor(_currencySymbol).Price;

            _initialTotalAmount = GetTotalAmount(balances, price);
        }

        private void Trade()
        {
            var orders = GetAllOrders();
            var lastOrder = _lastOrder != null ? orders.GetOrder(_lastOrder.OrderId) : null;

            //if (lastOrder != null && DateTime.Now - _lastOrderTime > TimeSpan.FromSeconds(20))
            //{
            //    var balances = GetBalances();
            //    var prices = _api.GetPrices().Result;
            //    var price = prices.PriceFor(_currencySymbol).Price;

            //    var totalAmount = GetTotalAmount(balances, price);

            //    var profit = CalculateProfit(_initialTotalAmount, totalAmount);
            //    if (profit > -5)
            //    {
            //        var cancelResult = _api.CancelOrder(_baseAsset, _quoteAsset, lastOrder.OrderId).Result;
            //        lastOrder = _lastOrder = null;

            //        Console.WriteLine("Order Canceled");
            //        Console.WriteLine();
            //    }
            //}

            if (lastOrder == null ||
                lastOrder.Status == OrderStatus.Canceled ||
                lastOrder.Status == OrderStatus.Expired)
            {
                StartTrading();
            }

            else if (lastOrder.Status == OrderStatus.Filled)
            {
                ContinueTrading(lastOrder);
            }
        }

        private void StartTrading()
        {
            var balances = GetBalances();
            var price = _api.GetPrices().Result.PriceFor(_currencySymbol).Price;

            if (CanPlaceSellOrder(balances, price, _minOrderAmount))
            {
                var amount = GetAmountToSell(balances);
                PlaceSellOrder(price, amount);
            }
            else if (CanPlaceBuyOrder(balances, _minOrderAmount))
            {
                var amount = GetAmountToBuy(price, balances);
                PlaceBuyOrder(price, amount);
            }
        }

        private void ContinueTrading(Order lastOrder)
        {
            var balances = GetBalances();

            LogOrderComplited(lastOrder.Side, lastOrder.Status, lastOrder.Price, balances);

            if (lastOrder.Side == OrderSide.Buy)
            {
                var price = (lastOrder.Price + lastOrder.Price.Percents(_fluctuation)).Round();
                if (CanPlaceSellOrder(balances, price, _minOrderAmount))
                {
                    var amount = GetAmountToSell(balances);
                    PlaceSellOrder(price, amount);
                }
            }
            else if (lastOrder.Side == OrderSide.Sell)
            {
                var price = (lastOrder.Price - lastOrder.Price.Percents(_fluctuation)).Round();
                if (CanPlaceBuyOrder(balances, _minOrderAmount))
                {
                    var amount = GetAmountToBuy(price, balances);
                    PlaceBuyOrder(price, amount);
                }
            }
        }

        private bool CanPlaceSellOrder(Balances balances, decimal price, decimal minOrderAmount)
        {
            var baseAmount = balances.GetBalanceFor(_baseAsset).Free;
            var amount = GetAmountToSell(balances);
            return baseAmount * price > minOrderAmount && amount > 0;
        }

        private bool CanPlaceBuyOrder(Balances balances, decimal minOrderAmount)
        {
            var quoteAmount = balances.GetBalanceFor(_quoteAsset).Free;

            return quoteAmount > minOrderAmount;
        }

        private bool PlaceBuyOrder(decimal price, decimal amount)
        {
            var succeed = HandleOrderRequest(
                OrderSide.Buy,
                TimeInForceType.GTC,
                price,
                amount,
                OrderStatus.New);

            return succeed;
        }

        private bool PlaceSellOrder(decimal price, decimal amount)
        {
            var succeed = HandleOrderRequest(
                OrderSide.Sell,
                TimeInForceType.GTC,
                price,
                amount,
                OrderStatus.New);

            return succeed;
        }

        private bool HandleOrderRequest(
            OrderSide side,
            TimeInForceType timeInForce,
            decimal price,
            decimal amount,
            OrderStatus succeedStatus)
        {
            var order = PlaceOrder(side, timeInForce, price, amount);
            var succeed = order.Status == succeedStatus;

            _lastOrder = order;
            _lastOrderTime = DateTime.Now;

            if (succeed)
            {
                LogOrderPlaced(side, order.Status, price);
            }

            return succeed;
        }

        private Order PlaceOrder(
            OrderSide side,
            TimeInForceType timeInForce,
            decimal price,
            decimal amount)
        {
            var orderConfig = new OrderConfig
            {
                BaseAsset = _baseAsset,
                QuoteAsset = _quoteAsset,
                Price = price,
                Quantity = amount,
                TimeInForce = timeInForce,
                Side = side,
                Type = OrderType.Limit
            };

            var order = _api.MakeOrder(orderConfig).Result;

            return order;
        }

        private void LogOrderPlaced(OrderSide side, OrderStatus status, decimal price)
        {
            _loger.LogOrderPlaced(side, status, price);
        }

        private void LogOrderComplited(OrderSide side, OrderStatus status, decimal price, Balances balances)
        {
            var baseAmount = balances.GetBalanceFor(_baseAsset).Free;
            var quoteAmount = balances.GetBalanceFor(_quoteAsset).Free;
            var totalAmount = GetTotalAmount(balances, price);
            var profit = MathUtils.CalculateProfit(_initialTotalAmount, totalAmount);

            _loger.LogOrderComplited(
                side,
                status,
                price,
                baseAmount,
                quoteAmount,
                totalAmount,
                profit);
        }

      

        //private static decimal CalculateProfit(decimal initialAmount, decimal price, decimal baseAmount, decimal fee)
        //{
        //    var qouteAmount = price * baseAmount;
        //    var netAmount = qouteAmount + qouteAmount.Percents(fee);

        //    return CalculateProfit(initialAmount, netAmount);
        //}

        private decimal GetTotalAmount(Balances balances, decimal basePrice)
        {
            var baseAmount = balances.GetBalanceFor(_baseAsset);
            var quoteAmount = balances.GetBalanceFor(_quoteAsset);
            //var feeAmount = balances.GetBalanceFor(_feeAsset);

            var total = quoteAmount.Free + quoteAmount.Locked +
                        (baseAmount.Free + baseAmount.Locked) * basePrice;
            //(feeAmount.Free + feeAmount.Locked) * feePrice;

            return total;
        }

        private Balances GetBalances()
        {
            return _api.GetAccountInfo().Result.Balances;
        }

        private decimal GetAmountToBuy(decimal price, Balances balances)
        {
            var quoteAmount = balances.GetBalanceFor(_quoteAsset).Free;
            var baseAmount = Math.Floor(quoteAmount / price);
            return baseAmount;
        }

        private decimal GetAmountToSell(Balances balances)
        {
            var baseAmount = Math.Floor(balances.GetBalanceFor(_baseAsset).Free);

            return baseAmount;
        }

        private Orders GetAllOrders()
        {
            return _api.GetAllOrders(_baseAsset, _quoteAsset, 10).Result;
        }
    }
}