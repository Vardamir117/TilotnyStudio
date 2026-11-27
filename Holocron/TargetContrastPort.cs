using System;
using System.Collections.Generic;

namespace Holocron
{
    // Port of the parts of TargetContrastClass (TargetContrast.cpp) that auto-resolve uses.
    //
    // ContrastTypeList in the game is a stdext::hash_map<unsigned __int64, SmartPtr<WeightedTypeListClass>>.
    // AutoResolveClass::Calculate_Side_Force walks it with begin()/end() and gives each key a result slot
    // (index 2 + iteration position), breaking at the first key that matches a unit's category mask.
    // The iteration order is therefore game behaviour, not an implementation detail, so this class
    // reproduces the MSVC 14.29 (VS2019 16.10/16.11) stdext::hash_map ordering used by StarWarsG/I.exe x64.
    public class TargetContrastPort
    {
        public class WeightedEntry
        {
            public ulong Category;      // non-zero when the Lua name resolved to a GameObjectCategoryType
            public string TypeName;     // set when the Lua name was neither a category nor a property
            public double Weight;
        }

        private readonly Dictionary<ulong, List<WeightedEntry>> mLists = new Dictionary<ulong, List<WeightedEntry>>();
        private readonly List<ulong> mIterationOrder = new List<ulong>();

        // Keys in stdext::hash_map iteration order.
        public IReadOnlyList<ulong> ContrastTypeList { get { return mIterationOrder; } }

        public bool Contains(ulong key) { return mLists.ContainsKey(key); }

        public List<WeightedEntry> Get_List(ulong key)
        {
            List<WeightedEntry> list;
            return mLists.TryGetValue(key, out list) ? list : null;
        }

        // Mirrors Parse_Lua_Contrast_String: keys are inserted in Lua order and hash_map::insert ignores a
        // key that is already present (the first definition wins).
        public static TargetContrastPort Build(IEnumerable<KeyValuePair<ulong, List<WeightedEntry>>> luaOrderedLists)
        {
            TargetContrastPort table = new TargetContrastPort();
            MsvcHashMapOrder order = new MsvcHashMapOrder();
            foreach (KeyValuePair<ulong, List<WeightedEntry>> pair in luaOrderedLists)
            {
                if (table.mLists.ContainsKey(pair.Key)) continue;
                table.mLists[pair.Key] = pair.Value ?? new List<WeightedEntry>();
                order.Insert(pair.Key);
            }
            table.mIterationOrder.AddRange(order.Keys);
            return table;
        }

        // TargetContrastClass::Get_Average_Contrast_Factor (x64 0x140035305). Weights are doubles.
        public double Get_Average_Contrast_Factor(AutoResolveObjectType type, ulong category)
        {
            List<WeightedEntry> weightList;
            if (type == null || !mLists.TryGetValue(category, out weightList)) return 0.0;

            double totalWeight = 0.0;
            int weightCount = 0;
            bool matchesContrast = false;
            for (int i = 0; i < weightList.Count; i++)
            {
                WeightedEntry entry = weightList[i];

                // Hand back the weight immediately if this type is mentioned explicitly.
                if (entry.TypeName != null && string.Equals(entry.TypeName, type.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Weight;
                }

                if ((type.CategoryMask & entry.Category) != 0UL)
                {
                    matchesContrast = true;
                    // Weights of exactly 1 mark compatibility but are left out of the average.
                    if (entry.Weight != 1.0)
                    {
                        totalWeight += entry.Weight;
                        ++weightCount;
                    }
                }
            }

            if (weightCount == 0) return matchesContrast ? 1.0 : 0.0;
            return totalWeight / weightCount;
        }

        // Reproduces insertion/iteration order of stdext::hash_map<unsigned __int64, T> in the MSVC STL
        // shipped with VS2019 16.10/16.11 (microsoft/STL tag vs-2019-16.10, stl/inc/xhash):
        //  - stdext::hash_compare::operator() = Park-Miller transform of (hash_value(key) & LONG_MAX), where
        //    hash_value(integer) = (size_t)key ^ 0xdeadbeef;
        //  - bucket = hash & (bucket_count - 1); 8 buckets initially, max_load_factor 1, grow x8 below 512;
        //  - a key whose bucket is empty is appended to the end of the element list, otherwise it is placed
        //    in its bucket's contiguous range, kept sorted ascending (std::less);
        //  - _Forced_rehash walks the list in order: the first element of each bucket stays put and later
        //    elements of the same bucket are spliced into that bucket's sorted range.
        public class MsvcHashMapOrder
        {
            private List<ulong> mKeys = new List<ulong>();
            private ulong mBuckets = 8;

            public IReadOnlyList<ulong> Keys { get { return mKeys; } }

            public static ulong Hash(ulong key)
            {
                ulong value = key ^ 0xdeadbeefUL;
                int quot = (int)(value & 0x7fffffffUL);           // static_cast<long>(... & LONG_MAX)
                int q = quot / 127773;
                int r = quot % 127773;
                int rem = 16807 * r - 2836 * q;
                if (rem < 0) rem += int.MaxValue;
                return (ulong)rem;
            }

            public void Insert(ulong key)
            {
                if (mKeys.Contains(key)) return;

                // _Check_rehash_required_1: max_load_factor (1.0) < (size + 1) / bucket_count
                if (1.0f < (float)(mKeys.Count + 1) / (float)mBuckets)
                {
                    ulong required = (ulong)Math.Ceiling((mKeys.Count + 1) / 1.0f);
                    ulong newBuckets = (mBuckets < 512 && mBuckets * 8 >= required) ? mBuckets * 8 : required;
                    ulong pow2 = 1;
                    while (pow2 < newBuckets) pow2 <<= 1;
                    mBuckets = pow2;
                    List<ulong> old = mKeys;
                    mKeys = new List<ulong>();
                    foreach (ulong existing in old) Place(existing);
                }

                Place(key);
            }

            private ulong Bucket(ulong key) { return Hash(key) & (mBuckets - 1); }

            private void Place(ulong key)
            {
                ulong bucket = Bucket(key);
                int hi = -1;
                for (int i = mKeys.Count - 1; i >= 0; i--)
                {
                    if (Bucket(mKeys[i]) == bucket) { hi = i; break; }
                }

                if (hi < 0)
                {
                    mKeys.Add(key);
                    return;
                }

                int lo = hi;
                while (lo > 0 && Bucket(mKeys[lo - 1]) == bucket) lo--;

                int insertAt = hi + 1;
                while (insertAt > lo && key < mKeys[insertAt - 1]) insertAt--;
                mKeys.Insert(insertAt, key);
            }
        }
    }
}
