using System;
using System.Collections.Generic;
using System.Linq;

namespace Holocron
{
    // Port helpers for TargetContrast.cpp behavior used by auto-resolve.
    public static class TargetContrastPort
    {
        // Mirrors TargetContrastClass::Get_Average_Contrast_Factor semantics for category-based contrast maps.
        public static float Get_Average_Contrast_Factor(
            IEnumerable<string> friendlyCategories,
            string enemyCategory,
            Func<string, string, float> contrastWeightProvider)
        {
            if (friendlyCategories == null || string.IsNullOrWhiteSpace(enemyCategory)) return 0.0f;

            float totalWeight = 0.0f;
            int weightCount = 0;
            bool matchesContrast = false;

            foreach (string friendlyCategory in friendlyCategories.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                float? maybeWeight = null;

                if (contrastWeightProvider != null)
                {
                    maybeWeight = contrastWeightProvider(enemyCategory, friendlyCategory);
                }

                if (!maybeWeight.HasValue) continue;

                float weight = maybeWeight.Value;
                matchesContrast = true;

                // C++ parity: ignore weights of exactly 1.0f when computing average,
                // but still mark that the contrast matched.
                if (Math.Abs(weight - 1.0f) > 0.0001f)
                {
                    totalWeight += weight;
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
