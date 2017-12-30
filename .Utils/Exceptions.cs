﻿using System;

namespace BinanceTrader.Utils
{
    public class NetworkException : Exception
    {
        public NetworkException(string message) : base(message)
        {
        }

        public NetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}