using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public static class AssetsProvider
    {
        [NotNull]
        [ItemNotNull]
        public static List<string> Assets =>
            new List<string>
            {
                "NCASH",
                "IOST",
                "STORM",
                "TRX",
                "FUN",
                "POE",
                "TNB",
                "XVG",
                "CDT",
                "DNT",
                "LEND",
                "MANA",
                "SNGLS",
                "TNT",
                "FUEL",
                "YOYO",
                "CND",
                "RCN",
                "MTH",
                "CMT",
                "SNT",
                "RPX",
                "ENJ",
                "CHAT",
                "BTS",
                "VIB",
                "SNM",
                "OST",
                "QSP",
                "DLT",
                "BAT"
            };
    }
}