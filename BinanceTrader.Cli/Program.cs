﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using BinanceTrader.Tools.KeyProviders;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var logger = new Logger();

            try
            {
                var keyProvider = new BlobKeyProvider("Rambler");
                var keys = keyProvider.GetKeys();
                var apiClient = new ApiClient(keys.Api, keys.Secret);
                var binanceClient = new BinanceClient(apiClient);

                var trader = new RabbitTrader(binanceClient, logger);
                trader.Start();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}