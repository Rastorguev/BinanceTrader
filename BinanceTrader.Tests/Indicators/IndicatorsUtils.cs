using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Indicators
{
    public static class IndicatorsUtils
    {
        public static bool CrossesFromAbove([NotNull] List<decimal> ma1, [NotNull] List<decimal> ma2)
        {
            if (ma1.Count != ma2.Count || ma1.Count < 3)
            {
                return false;
            }

            var currentIndex = ma1.Count - 1;
            var prevIndex = ma1.Count - 2;
            var lastBut2Index = ma1.Count - 3;

            return ma1[currentIndex] - ma2[currentIndex] < 0 &&
                   ma1[prevIndex] - ma2[prevIndex] >= 0 &&
                   ma1[lastBut2Index] - ma2[lastBut2Index] > 0;
        }

        public static bool CrossesFromBelow([NotNull] List<decimal> ma1, [NotNull] List<decimal> ma2)
        {
            if (ma1.Count != ma2.Count || ma1.Count < 3)
            {
                return false;
            }

            var currentIndex = ma1.Count - 1;
            var prevIndex = ma1.Count - 2;
            var lastBut2Index = ma1.Count - 3;

            return ma1[currentIndex] - ma2[currentIndex] > 0 &&
                   ma1[prevIndex] - ma2[prevIndex] <= 0 &&
                   ma1[lastBut2Index] - ma2[lastBut2Index] < 0;
        }
    }
}