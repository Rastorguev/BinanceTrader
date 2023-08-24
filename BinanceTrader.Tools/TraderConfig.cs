namespace BinanceTrader.Tools;

public class TraderConfig
{
    public string Name { get; set; }

    public bool IsEnabled { get; set; }

    public IReadOnlyList<string> BaseAssets { get; } = new List<string>();

    public string QuoteAsset { get; set; }

    public TimeSpan OrderExpiration { get; set; }
    public decimal ProfitRatio { get; set; }
}