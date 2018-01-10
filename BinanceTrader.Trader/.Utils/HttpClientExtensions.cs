using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BinanceTrader.Utils
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetAsync<T>([NotNull] this HttpClient client, Uri uri)
        {
            var response = await client.GetAsync(uri).NotNull();
            var json = response.NotNull().Content.NotNull().ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.NotNull().ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<T> PostAsync<T>([NotNull] this HttpClient client, Uri uri)
        {
            var response = await client.PostAsync(uri, new ByteArrayContent(new byte[0])).NotNull();
            var json = response.NotNull().Content.NotNull().ReadAsStringAsync().Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.NotNull().ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<T> DeleteAsync<T>([NotNull] this HttpClient client, Uri uri)
        {
            var response = await client.DeleteAsync(uri).NotNull();
            var json = response.NotNull().Content.NotNull().ReadAsStringAsync().Result.NotNull();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BinanceApiException(response.Content.NotNull().ReadAsStringAsync().Result);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}