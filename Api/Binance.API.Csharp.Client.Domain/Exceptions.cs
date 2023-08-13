using System;

namespace Binance.API.Csharp.Client.Domain
{
    public class BinanceApiException : Exception
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

    public class InvalidRequestException : BinanceApiException
    {
        public InvalidRequestException()
        {
        }

        public InvalidRequestException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public InvalidRequestException(int errorCode, string message, Exception innerException) : base(message,
            innerException)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }
    }

    public class InsufficientBalanceException : InvalidRequestException
    {
        public InsufficientBalanceException(int errorCode, string message) : base(errorCode, message)
        {
        }

        public InsufficientBalanceException(int errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
        }
    }

    public class UnknownOrderException : InvalidRequestException
    {
        public UnknownOrderException(int errorCode, string message) : base(errorCode, message)
        {
        }

        public UnknownOrderException(int errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
        }
    }
}