using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Analysis.Indicator;
using Trady.Core.Infrastructure;

namespace BinanceTrader.Analytics
{
    public static class IndexedCandleExtensions
    {
        public static bool IsDmiBullishCross([NotNull] this IIndexedOhlcv ic, int periodCount)
        {
            var (plusPrev, plusCurrent, _) =
                ic.Get<PlusDirectionalIndicator>(periodCount).NotNull().ComputeNeighbour(ic.Index);

            var (minusPrev, minusCurrent, _) =
                ic.Get<MinusDirectionalIndicator>(periodCount).NotNull().ComputeNeighbour(ic.Index);

            if (plusPrev == null ||
                plusCurrent == null ||
                minusPrev == null ||
                minusCurrent == null ||
                plusPrev.Tick == null ||
                plusCurrent.Tick == null ||
                minusPrev.Tick == null ||
                minusCurrent.Tick == null)
            {
                return false;
            }

            return plusPrev.Tick.Value - minusPrev.Tick.Value <= 0 &&
                   plusCurrent.Tick.Value - minusCurrent.Tick.Value > 0;
        }

        public static bool IsDmiBearishCross([NotNull] this IIndexedOhlcv ic, int periodCount)
        {
            var (plusPrev, plusCurrent, _) =
                ic.Get<PlusDirectionalIndicator>(periodCount).NotNull().ComputeNeighbour(ic.Index);

            var (minusPrev, minusCurrent, _) =
                ic.Get<MinusDirectionalIndicator>(periodCount).NotNull().ComputeNeighbour(ic.Index);

            if (plusPrev == null ||
                plusCurrent == null ||
                minusPrev == null ||
                minusCurrent == null ||
                plusPrev.Tick == null ||
                plusCurrent.Tick == null ||
                minusPrev.Tick == null ||
                minusCurrent.Tick == null)
            {
                return false;
            }

            return plusPrev.Tick.Value - minusPrev.Tick.Value >= 0 &&
                   plusCurrent.Tick.Value - minusCurrent.Tick.Value < 0;
        }
    }
}