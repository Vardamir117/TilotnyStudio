using System;
using System.Collections.Generic;
using System.Linq;

namespace Holocron
{
    // Port helpers for TargetContrast.cpp behavior used by auto-resolve.
    public static class TargetContrastPort
    {
        public struct WeightedCategoryEntry
        {
            public ulong CategoryMask;
            public float Weight;
        }

        // Mirrors TargetContrastClass::Get_Average_Contrast_Factor semantics for bitmask categories.
        public static float Get_Average_Contrast_Factor(
            ulong friendlyCategoryMask,
            List<WeightedCategoryEntry> weightList)
        {
            if (friendlyCategoryMask == 0UL || weightList == null || weightList.Count == 0) return 0.0f;

            float totalWeight = 0.0f;
            int weightCount = 0;
            bool matchesContrast = false;

            for (int i = 0; i < weightList.Count; i++)
            {
                WeightedCategoryEntry entry = weightList[i];
                if ((friendlyCategoryMask & entry.CategoryMask) == 0UL) continue;

                matchesContrast = true;

                // C++ parity: ignore weights of exactly 1.0f when computing average,
                // but still mark that the contrast matched.
                if (Math.Abs(entry.Weight - 1.0f) > 0.0001f)
                {
                    totalWeight += entry.Weight;
                    weightCount++;
                }
            }

            if (weightCount == 0)
            {
                if (matchesContrast) return 1.0f;
                return 0.0f;
            }

            return totalWeight / weightCount;
        }
    }
}
