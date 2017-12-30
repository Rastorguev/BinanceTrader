using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BinanceTrader.Utils;

namespace BinanceTrader.Api
{
    public class BinanceApi
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly Uri _baseUri = new Uri("https://www.binance.com/api/");
        private readonly Uri _pricesUri;
        private readonly Uri _accountInfoUri;

        private readonly string _secretKey;

        public BinanceApi(IBinanceKeyProvider binanceKeyProvider)
        {
            var keys = binanceKeyProvider.GetKeys();
            var apiKey = keys.ApiKey;
            _secretKey = keys.SecretKey;

            _pricesUri = new Uri(_baseUri, "v1/ticker/allPrices");
            _accountInfoUri = new Uri(_baseUri, "v3/account");
            _client.DefaultRequestHeaders.TryAddWithoutValidation("X-MBX-APIKEY", apiKey);
        }

        public async Task<List<CurrencyPrice>> GetPrices()
        {
            var prices = await _client.Execute<List<CurrencyPrice>>(_pricesUri);
            return prices;
        }

        public async Task<string> GetAccountInfo()
        {
            var result = await _client.GetAsync(CreateSignedUri());
            var content = await result.Content.ReadAsStringAsync();

            return content;
        }

        public Uri CreateSignedUri(NameValueCollection queryParams = null)
        {
            queryParams = queryParams ?? new NameValueCollection();
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var uriBuilder = new UriBuilder(_accountInfoUri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (string key in queryParams)
            {
                query.Add(key, queryParams.Get(key));
            }

            query["recvWindow"] = "5000";
            query["timestamp"] = timestamp.ToString();
            query["signature"] = CreateSignature(query.ToString(), _secretKey);

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        private static string CreateSignature(string data, string secretKey)
        {
            var key = Encoding.ASCII.GetBytes(secretKey);
            using (var hmac = new HMACSHA256(key))
            {
                var payload = Encoding.ASCII.GetBytes(data);
                var hash = hmac.ComputeHash(payload, 0, payload.Length);

                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}