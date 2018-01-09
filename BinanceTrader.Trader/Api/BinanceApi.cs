using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using BinanceTrader.Entities;
using BinanceTrader.Utils;

namespace BinanceTrader.Api
{
    public class BinanceApi
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _secretKey;
        private readonly Uri _baseUri = new Uri("https://www.binance.com/api/");
        private readonly Uri _pricesUri;
        private readonly Uri _candlesUri;
        private readonly Uri _accountInfoUri;
        private readonly Uri _orderUri;
        private readonly Uri _openOrdersUri;
        private readonly Uri _allOrdersUri;

        public BinanceApi(IBinanceKeyProvider binanceKeyProvider)
        {
            var keys = binanceKeyProvider.GetKeys();
            _secretKey = keys.SecretKey;
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-MBX-APIKEY", keys.ApiKey);

            _candlesUri = new Uri(_baseUri, "v1/klines");
            _pricesUri = new Uri(_baseUri, "v1/ticker/allPrices");

            _accountInfoUri = new Uri(_baseUri, "v3/account");
            _orderUri = new Uri(_baseUri, "v3/order");
            _openOrdersUri = new Uri(_baseUri, "v3/openOrders");
            _allOrdersUri = new Uri(_baseUri, "v3/allOrders");
        }

        #region MarketData

        public async Task<CurrencyPrices> GetPrices()
        {
            var prices = await _client.GetAsync<CurrencyPrices>(_pricesUri);
            return prices;
        }

        public async Task<CandlesChart> GetCandles(
            string baseAsset,
            string quoteAsset,
            string interval,
            int limit = 100)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset)},
                {"interval", interval}
            };

            if (limit != 0)
            {
                queryParams.Add("limit", limit.ToString(CultureInfo.InvariantCulture));
            }

            var candles = (
                    await _client.GetAsync<List<List<object>>>(CreateUri(_candlesUri, queryParams))
                        .NotNull())
                .ToCandles();
            return candles;
        }

        public async Task<CandlesChart> GetCandles(
            string baseAsset,
            string quoteAsset,
            string interval, 
            DateTime startTime, 
            DateTime endTime)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset)},
                {"interval", interval},
                {"startTime", new DateTimeOffset(startTime).ToUnixTimeMilliseconds().ToString()},
                {"endTime",  new DateTimeOffset(endTime).ToUnixTimeMilliseconds().ToString()}
            };

            var candles = (
                    await _client.GetAsync<List<List<object>>>(CreateUri(_candlesUri, queryParams))
                        .NotNull())
                .ToCandles();
            return candles;
        }

        #endregion

        #region Account

        public async Task<AccountInfo> GetAccountInfo()
        {
            return await _client.GetAsync<AccountInfo>(CreateSignedUri(_accountInfoUri));
        }

        public async Task<Order> MakeOrder(OrderConfig config)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(config.BaseAsset, config.QuoteAsset)},
                {"side", config.Side.ToRequestParam()},
                {"type", config.Type.ToRequestParam()},
                {"timeInForce", config.TimeInForce.ToRequestParam()},
                {"quantity", config.Quantity.ToString(CultureInfo.InvariantCulture)},
                {"price", config.Price.ToString(CultureInfo.InvariantCulture)}
            };

            var result = await _client.PostAsync<Order>(
                CreateSignedUri(_orderUri, queryParams));

            return result;
        }

        public async Task<Orders> GetOpenOrders(string baseAsset, string quoteAsset)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset)}
            };

            var result = await _client.GetAsync<Orders>(CreateSignedUri(_openOrdersUri, queryParams));

            return result;
        }

        public async Task<Orders> GetAllOrders(string baseAsset, string quoteAsset, int limit = 500)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset)},
                {"limit", limit.ToString(CultureInfo.InvariantCulture)}
            };

            var result = await _client.GetAsync<Orders>(CreateSignedUri(_allOrdersUri, queryParams));

            return result;
        }

        public async Task<Order> CancelOrder(string baseAsset, string quoteAsset, long id)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(baseAsset, quoteAsset)},
                {"orderId", id.ToString(CultureInfo.InvariantCulture)}
            };

            var result = await _client.DeleteAsync<Order>(CreateSignedUri(_orderUri, queryParams));

            return result;
        }

        #endregion

        private Uri CreateSignedUri(Uri uri, NameValueCollection queryParams = null)
        {
            queryParams = queryParams ?? new NameValueCollection();
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (string key in queryParams)
            {
                query.Add(key, queryParams.Get(key));
            }

            query["recvWindow"] = "5000";
            query["timestamp"] = (timestamp - 1000).ToString();
            query["signature"] = UrlEncoder.CreateHash(query.ToString(), _secretKey);

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        private Uri CreateUri(Uri uri, NameValueCollection queryParams = null)
        {
            queryParams = queryParams ?? new NameValueCollection();
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (string key in queryParams)
            {
                query.Add(key, queryParams.Get(key));
            }

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }
    }


    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentUnixTimestampMillis(this DateTime time)
        {
            return (long)(time.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }

        //public static DateTime DateTimeFromUnixTimestampMillis(long millis)
        //{
        //    return UnixEpoch.AddMilliseconds(millis);
        //}

        //public static long GetCurrentUnixTimestampSeconds()
        //{
        //    return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        //}

        //public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
        //{
        //    return UnixEpoch.AddSeconds(seconds);
        //}
    }
}