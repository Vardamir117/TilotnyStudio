using System.Collections.Generic;

namespace Holocron
{
    // Simplified equivalent of TargetContrastClass::ResultType.
    // Mirrors C++ ContrastForceStruct list shape: category + force + ground.
    public struct TargetResult
    {
        public class ContrastForceStruct
        {
            public ulong Category;
            public float Force;
            public bool Ground;

            public ContrastForceStruct(ulong category, float force, bool ground)
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

        public TargetResult Clone()
        {
            TargetResult result = new TargetResult();
            result.Entries = new List<ContrastForceStruct>(Count);
            for (int i = 0; i < Count; i++) result.Entries.Add(this[i].Clone());
            return result;
        }
    }
}
