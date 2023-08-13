namespace Binance.API.Csharp.Client.Utils
{
    /// <summary>
    /// Public class to store all end points.
    /// </summary>
    public static class EndPoints
    {
        #region General Endpoints
        public static readonly string TestConnectivity = "/api/v1/ping";
        public static readonly string CheckServerTime = "/api/v1/time";
        #endregion

        #region Market Data Endpoints
        public static readonly string OrderBook = "/api/v3/depth";
        public static readonly string AggregateTrades = "/api/v3/aggTrades";
        public static readonly string Candlesticks = "/api/v3/klines";
        public static readonly string TickerPriceChange24H = "/api/v3/ticker/24hr";
        public static readonly string Price = "/api/v3/ticker/price";
        public static readonly string OrderBookTicker = "/api/v3/ticker/allBookTickers";
        public static readonly string TradingRules = "/api/v3/exchangeInfo";
        #endregion

        #region Account Endpoints
        public static readonly string NewOrder = "/api/v3/order";
        public static readonly string NewOrderTest = "/api/v3/order/test";
        public static readonly string QueryOrder = "/api/v3/order";
        public static readonly string CancelOrder = "/api/v3/order";
        public static readonly string CurrentOpenOrders = "/api/v3/openOrders";
        public static readonly string AllOrders = "/api/v3/allOrders";
        public static readonly string AccountInformation = "/api/v3/account";
        public static readonly string TradeList = "/api/v3/myTrades";

        public static readonly string Withdraw = "/wapi/v1/withdraw.html";
        public static readonly string DepositHistory = "/wapi/v1/getDepositHistory.html";
        public static readonly string WithdrawHistory = "/wapi/v1/getWithdrawHistory.html";
        #endregion

        #region User Stream Endpoints
        public static readonly string StartUserStream = "/api/v3/userDataStream";
        public static readonly string KeepAliveUserStream = "/api/v3/userDataStream";
        public static readonly string CloseUserStream = "/api/v3/userDataStream";
        #endregion
    }
}
