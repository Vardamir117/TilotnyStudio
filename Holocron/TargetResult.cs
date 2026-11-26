using System.Collections.Generic;

namespace Holocron
{
    // Equivalent of TargetContrastClass::ResultType (std::vector<ContrastForceStruct>).
    // In the x64 build ContrastForceStruct::Force is a double (all accumulations convert float power
    // with cvtss2sd), so Force is kept as double here.
    public struct TargetResult
    {
        public class ContrastForceStruct
        {
            public ulong Category;
            public double Force;
            public bool Ground;

            public ContrastForceStruct(ulong category, double force, bool ground)
            {
                Category = category;
                Force = force;
                Ground = ground;
            }

            public ContrastForceStruct Clone()
            {
                return new ContrastForceStruct(Category, Force, Ground);
            }
        }

        public List<ContrastForceStruct> Entries;

        public int Count
        {
            get { return Entries == null ? 0 : Entries.Count; }
        }

        public ContrastForceStruct this[int index]
        {
            get { return Entries[index]; }
            set { Entries[index] = value; }
        }

        // resize(0); resize(n): default-constructed entries are Category 0, Force 0, Ground false.
        public static TargetResult Create(int size)
        {
            TargetResult result = new TargetResult();
            result.Entries = new List<ContrastForceStruct>(size);
            for (int i = 0; i < size; i++) result.Entries.Add(new ContrastForceStruct(0UL, 0.0, false));
            return result;
        }

        public TargetResult Clone()
        {
            TargetResult result = new TargetResult();
            result.Entries = new List<ContrastForceStruct>(Count);
            for (int i = 0; i < Count; i++) result.Entries.Add(this[i].Clone());
            return result;
        }
    }
}
