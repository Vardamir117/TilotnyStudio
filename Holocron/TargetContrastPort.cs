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
            public string TypeName;
            public ulong CategoryMask;
            public float Weight;
        }

        // Mirrors exact-type priority and category averaging from Get_Average_Contrast_Factor.
        public static float Get_Average_Contrast_Factor(
            string friendlyTypeName,
            ulong friendlyCategoryMask,
            List<WeightedCategoryEntry> weightList)
        {
            if (weightList == null || weightList.Count == 0) return 0.0f;

            for (int i = 0; i < weightList.Count; i++)
            {
                WeightedCategoryEntry entry = weightList[i];
                if (!string.IsNullOrWhiteSpace(entry.TypeName) &&
                    string.Equals(entry.TypeName, friendlyTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Weight;
                }
            }

            if (friendlyCategoryMask == 0UL) return 0.0f;

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
                if (entry.Weight != 1.0f)
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
