using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BinanceTrader.Utils
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetAsync<T>(this HttpClient client, Uri uri)
        {
            var response = await client.GetAsync(uri);
            var json = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<T> PostAsync<T>(this HttpClient client, Uri uri)
        {
            var response = await client.PostAsync(uri, new ByteArrayContent(new byte[0]));
            var json = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<T> DeleteAsync<T>(this HttpClient client, Uri uri)
        {
            var response = await client.DeleteAsync(uri);
            var json = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

    }
}