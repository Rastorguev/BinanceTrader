using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BinanceTrader.Utils
{
    public static class HttpClientExtensions
    {
        public static async Task<T> Execute<T>(this HttpClient client, Uri uri)
        {
            var response = await client.GetAsync(uri);
            var json = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetworkException(response.Content.ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}