using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Market.TradingRules;
using BinanceTrader.Tools;

namespace BinanceTrader.Core;

public class TradingRulesProvider
{
    private readonly IBinanceClient _client;

    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10);
    private DateTime? _lastUpdateTime;
    private TradingRulesContainer _rulesContainer;

    public TradingRulesProvider(IBinanceClient client)
    {
        _client = client;
    }

    public IReadOnlyList<TradingRules> Rules => _rulesContainer?.Rules.ToList() ?? new List<TradingRules>();

    public async Task UpdateRulesIfNeeded()
    {
        try
        {
            var isValid = _rulesContainer != null &&
                          _lastUpdateTime != null &&
                          _lastUpdateTime.Value + _expiration > DateTime.Now;

            if (!isValid)
            {
                _rulesContainer = await _client.LoadTradingRules();
                _lastUpdateTime = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            throw new AppException("Unable to load trading rules", ex);
        }
    }

    public TradingRules GetRulesFor(string symbol)
    {
        try
        {
            return _rulesContainer.Rules.First(s => s.Symbol == symbol);
        }
        catch (Exception ex)
        {
            throw new AppException($"Trading rules for {symbol} are not found", ex);
        }
    }

    public List<string> GetBaseAssetsFor(string asset)
    {
        return _rulesContainer.Rules.Where(r => r.QuoteAsset == asset)
            .Select(r => r.BaseAsset).ToList();
    }
}