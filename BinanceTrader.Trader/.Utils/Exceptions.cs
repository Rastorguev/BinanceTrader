using System;

namespace BinanceTrader.Utils
{
    public class AppException : Exception
    {
        public AppException()
        {
        }

        public AppException(string message) : base(message)
        {
        }

        public AppException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class InsufficientBalanceException : AppException
    {
        public InsufficientBalanceException()
        {
        }

        public InsufficientBalanceException(string message) : base(message)
        {
        }

        public InsufficientBalanceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class BinanceApiException : AppException
    {
        public BinanceApiException()
        {
        }

        public BinanceApiException(string message) : base(message)
        {
        }

        public BinanceApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}