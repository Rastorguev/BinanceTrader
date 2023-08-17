using JetBrains.Annotations;

namespace BinanceTrader;

public static class AssetsProvider
{
    [NotNull]
    [ItemNotNull]
    public static IReadOnlyList<string> Assets =>
        new List<string>
        {
            "BCN",
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
            "WPR",
            "CMT",
            "SNT",
            "RPX",
            "ENJ",
            "ZIL",
            "QLC",
            "CHAT",
            "BTS",
            "VIB",
            "SNM",
            "OST",
            "REQ",
            "VIBE",
            "QSP",
            "DLT",
            "BAT",
            "ADA",
            "GTO",
            "XEM",
            "AST",
            "XLM"
        };
}