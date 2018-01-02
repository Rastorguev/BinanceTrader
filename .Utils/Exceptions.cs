using System;

namespace BinanceTrader.Utils
{
    public class BinaceApiException : Exception
    {
        public BinaceApiException(string message) : base(message)
        {
        }

        public BinaceApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}