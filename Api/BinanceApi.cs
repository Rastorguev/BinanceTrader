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
        private readonly Uri _accountInfoUri;
        private readonly Uri _testOrderUri;

        public BinanceApi(IBinanceKeyProvider binanceKeyProvider)
        {
            var keys = binanceKeyProvider.GetKeys();
            _secretKey = keys.SecretKey;
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-MBX-APIKEY", keys.ApiKey);

            _pricesUri = new Uri(_baseUri, "v1/ticker/allPrices");
            _accountInfoUri = new Uri(_baseUri, "v3/account");
            _testOrderUri = new Uri(_baseUri, "v3/order");
        }

        public async Task<List<CurrencyPrice>> GetPrices()
        {
            var prices = await _client.GetAsync<List<CurrencyPrice>>(_pricesUri);
            return prices;
        }

        public async Task<AccountInfo> GetAccountInfo()
        {
            return await _client.GetAsync<AccountInfo>(CreateSignedUri(_accountInfoUri));
        }

        public async Task<OrderResult> CreateOrder(OrderConfig config)
        {
            var queryParams = new NameValueCollection
            {
                {"symbol", ApiUtils.CreateCurrencySymbol(config.BaseCurrency, config.QuoteCurrency)},
                {"side", config.Side.ToRequestParam()},
                {"type", config.Type.ToRequestParam()},
                {"timeInForce", config.TimeInForce.ToRequestParam()},
                {"quantity", config.Quantity.ToString(CultureInfo.InvariantCulture)},
                {"price", config.Price.ToString(CultureInfo.InvariantCulture)}
            };

            var result = await _client.PostAsync<OrderResult>(
                CreateSignedUri(_testOrderUri, queryParams));

            return result;
        }

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
    }
}