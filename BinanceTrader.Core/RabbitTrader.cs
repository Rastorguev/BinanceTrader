﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private readonly TimeSpan _scheduleInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _sellWaitingTime = TimeSpan.FromHours(1);
        private readonly TimeSpan _buyWaitingTime = TimeSpan.FromHours(1);
        private const decimal ProfitRatio = 0.5m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";

        private const decimal MinOrderSize = 0.015m;

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private IReadOnlyList<string> _assets = new List<string>();
        [NotNull] private readonly FundsStateChecker _fundsStateChecker;

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);
            _fundsStateChecker = new FundsStateChecker(_client, _logger, QuoteAsset);
        }

        public async void Start()
        {
            while (true)
            {
                await Task.WhenAll(new List<Task> {ExecuteScheduledTasks(), Task.Delay(_scheduleInterval)}).NotNull();
            }

            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        public async Task ExecuteScheduledTasks()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();

                await BuyFeeCurrencyIfNeeded();
                await CheckOrders();

                _fundsStateChecker.Assets = _assets;
                await _fundsStateChecker.LogFundsStateIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task CheckOrders()
        {
            await CancelExpiredOrders();
            await PlaceSellOrders();
            await PlaceBuyOrders();
        }

        private async Task CancelExpiredOrders()
        {
            try
            {
                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();
                var now = DateTime.Now;
                var expiredSellOrders = openOrders
                    .Where(o => o.NotNull().Side == OrderSide.Sell &&
                                now.ToLocalTime() - o.NotNull().UnixTime.GetTime().ToLocalTime() > _sellWaitingTime)
                    .ToList();
                var expiredBuyOrders = openOrders
                    .Where(o => o.NotNull().Side == OrderSide.Buy &&
                                now.ToLocalTime() - o.NotNull().UnixTime.GetTime().ToLocalTime() > _buyWaitingTime)
                    .ToList();

                foreach (var order in expiredSellOrders.Concat(expiredBuyOrders))
                {
                    await CancelOrder(order.NotNull());
                    _logger.LogOrder("Canceled", order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task PlaceSellOrders()
        {
            try
            {
                var freeBalances
                    = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull()
                    .Where(b => b.NotNull().Free > 0 && _assets.Contains(b.Asset)).ToList();

                foreach (var balance in freeBalances)
                {
                    try
                    {
                        var symbol = SymbolUtils.GetCurrencySymbol(balance.NotNull().Asset.NotNull(), QuoteAsset);
                        var price = await GetActualPrice(symbol, OrderSide.Sell);
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);

                        var sellAmounts =
                            OrderDistributor.SplitIntoSellOrders(
                                balance.Free,
                                MinOrderSize,
                                price,
                                tradingRules.StepSize);

                        foreach (var amount in sellAmounts)
                        {
                            var sellPrice =
                                AdjustPriceAccordingRules(price + price.Percents(ProfitRatio), tradingRules);
                            var orderRequest = new OrderRequest(symbol, OrderSide.Sell, amount, sellPrice);

                            if (MeetsTradingRules(orderRequest))
                            {
                                await PlaceOrder(orderRequest);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task PlaceBuyOrders()
        {
            try
            {
                var freeQuoteBalance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull()
                    .First(b => b.NotNull().Asset == QuoteAsset).NotNull().Free;

                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();

                var openOrdersCount = _assets.Select(asset =>
                    {
                        var symbol = SymbolUtils.GetCurrencySymbol(asset, QuoteAsset);
                        var count = openOrders.Count(o => o.NotNull().Symbol == symbol);
                        return (symbol: symbol, count: count);
                    })
                    .ToDictionary(x => x.symbol, x => x.count);

                var amounts =
                    OrderDistributor.SplitIntoBuyOrders(freeQuoteBalance, MinOrderSize, openOrdersCount);

                foreach (var symbolAmounts in amounts)
                {
                    try
                    {
                        var symbol = symbolAmounts.Key;
                        var price = await GetActualPrice(symbol, OrderSide.Buy);
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);
                        var buyAmounts = symbolAmounts.Value.NotNull();

                        foreach (var quoteAmount in buyAmounts)
                        {
                            var buyPrice = AdjustPriceAccordingRules(price - price.Percents(ProfitRatio), tradingRules);
                            var baseAmount =
                                OrderDistributor.GetFittingBaseAmount(quoteAmount, buyPrice, tradingRules.StepSize);
                            var orderRequest = new OrderRequest(symbol, OrderSide.Buy, baseAmount, buyPrice);

                            if (MeetsTradingRules(orderRequest))
                            {
                                await PlaceOrder(orderRequest);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task<decimal> GetActualPrice(string symbol, OrderSide orderSide)
        {
            var priceInfo = await GetPrices(symbol).NotNull();

            switch (orderSide)
            {
                case OrderSide.Buy:
                    return priceInfo.AskPrice;
                case OrderSide.Sell:
                    return priceInfo.BidPrice;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderSide), orderSide, null);
            }
        }

        private async Task<NewOrder> PlaceOrder([NotNull] OrderRequest orderRequest,
            OrderType orderType = OrderType.Limit,
            TimeInForce timeInForce = TimeInForce.GTC)
        {
            var newOrder = (await _client.PostNewOrder(
                        orderRequest.Symbol,
                        orderRequest.Qty,
                        orderRequest.Price,
                        orderRequest.Side,
                        orderType,
                        timeInForce)
                    .NotNull())
                .NotNull();

            _logger.LogOrder("Placed", newOrder);

            return newOrder;
        }

        private bool MeetsTradingRules([NotNull] OrderRequest orderRequest)
        {
            var rules = _rulesProvider.GetRulesFor(orderRequest.Symbol);
            if (orderRequest.MeetsTradingRules(rules))
            {
                return true;
            }

            _logger.LogOrderRequest("OrderRequestDoesNotMeetRules", orderRequest);

            return false;
        }

        private async Task<PriceChangeInfo> GetPrices(string symbol)
        {
            var priceInfo = (await _client.GetPriceChange24H(symbol).NotNull()).NotNull().First();

            return priceInfo;
        }

        private async Task<CanceledOrder> CancelOrder([NotNull] IOrder order)
        {
            var canceledOrder = await _client.CancelOrder(order.Symbol, order.OrderId).NotNull();

            _logger.LogOrder("Canceled", order);

            return canceledOrder;
        }

        private async Task BuyFeeCurrencyIfNeeded()
        {
            try
            {
                const int qty = 1;
                var feeSymbol = SymbolUtils.GetCurrencySymbol(FeeAsset, QuoteAsset);

                var balance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull().ToList();

                var feeAmount = balance.First(b => b.NotNull().Asset == FeeAsset).NotNull().Free;
                var quoteAmount = balance.First(b => b.NotNull().Asset == QuoteAsset).NotNull().Free;
                var price = await GetActualPrice(feeSymbol, OrderSide.Buy);

                if (feeAmount < 1 && quoteAmount >= price * qty)
                {
                    var orderRequest = new OrderRequest(feeSymbol, OrderSide.Buy, qty, price);

                    if (MeetsTradingRules(orderRequest))
                    {
                        var order = await PlaceOrder(orderRequest, OrderType.Market, TimeInForce.IOC).NotNull();
                        var status = order.Status;
                        var executedQty = order.ExecutedQty;

                        _logger.LogMessage("BuyFeeCurrency",
                            $"Status {status}, Quantity {executedQty}, Price {price}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private static decimal AdjustPriceAccordingRules(decimal price, [NotNull] ITradingRules rules) =>
            (int) (price / rules.TickSize) * rules.TickSize;
    }
}