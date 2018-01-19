using System;
using System.Security.Cryptography;
using System.Text;

namespace BinanceTrader.Api
{
    public static class UrlEncoder
    {
        public static string CreateHash(string url, string secretKey)
        {
            var key = Encoding.ASCII.GetBytes(secretKey);
            using (var hmac = new HMACSHA256(key))
            {
                var payload = Encoding.ASCII.GetBytes(url);
                var hash = hmac.ComputeHash(payload, 0, payload.Length);

                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}