using System;
using System.Collections.Generic;
using System.Linq;

namespace Holocron
{
    // Minimal HRESULT-style status values used by the ported auto-resolve flow.
    public enum AutoResolveHResult
    {
        S_OK = 0,
        E_AUTORESOLVE_NOT_READY = 1,
        E_AUTORESOLVE_COMBAT_STARTED = 2,
        E_POINTER = 3,
        E_AUTORESOLVE_BAD_OBJECT = 4
    }

    public enum MapEnvironmentType
    {
        MapTypeInvalid,
        Space,
        Ground
    }

    // Mirrors AutoResolveKilled from AutoResolve.h: object type + owning player.
    public class AutoResolveKilled
    {
        public string ObjectType;
        public int Owner;
    }

    // Mirrors AutoResolveBattle from AutoResolve.h: battle location and kill log.
    public class AutoResolveBattle
    {
        public string Planet;
        public List<KeyValuePair<string, int>> Killed = new List<KeyValuePair<string, int>>();
    }

    // Simplified combatant abstraction used by the C# port.
    // Maps to behavior/type data consumed by AutoResolveClass in C++.
    public class AutoResolveCombatant
    {
        public string TypeName;
        public int OwnerId;
        public bool IsEscort;
        public bool CanRetreat = true;
        public bool IsTransport;
        public bool IsAlive = true;
        public float Health = 1.0f;
        public float Power = 1.0f;

        public List<string> ContrastCategories = new List<string>();

        public float HealthRatio
        {
            get { return Math.Max(0f, Math.Min(1f, Health)); }
        }
    }

    public class AutoResolveRoundReport
    {
        public int RoundNumber;
        public string SideAUnit;
        public string SideBUnit;
        public float SideAPower;
        public float SideBPower;
        public int WinnerSideIndex;
        public int WinnerOwnerId;
        public string WinningUnit;
        public string LosingUnit;
        public bool LosingUnitDestroyed;
    }

    // C# port of AutoResolveClass from AutoResolve.h/.cpp.
    // This version preserves the main call flow and data shape used by the original system.
    public class AutoResolveClass
    {
        // Matches MAX_HISTORY in AutoResolve.h.
        private const int MAX_HISTORY = 8;

        // Mirrors SideStruct from AutoResolve.h: owner, frontline unit/escort, queue, and weakest unit tracking.
        private class SideStruct
        {
            public int OwnerId = -1;
            public AutoResolveCombatant Unit;
            public AutoResolveCombatant Escort;
            public List<AutoResolveCombatant> Queue = new List<AutoResolveCombatant>();
            public AutoResolveCombatant WeakestUnit;

            public int UnitCount
            {
                get { return Queue.Count(x => x.IsAlive); }
            }

            public void Init()
            {
                Unit = null;
                Escort = null;
                Queue.Clear();
                WeakestUnit = null;
            }

            public void AddCombatant(AutoResolveCombatant combatant)
            {
                Queue.Add(combatant);
                SortQueue();
                UpdatePositions();
            }

            public void SortQueue()
            {
                Queue = Queue.OrderByDescending(x => x.IsAlive).ThenByDescending(x => x.Power).ToList();
            }

            public void UpdatePositions()
            {
                Unit = Queue.FirstOrDefault(x => x.IsAlive && !x.IsEscort);
                Escort = Queue.FirstOrDefault(x => x.IsAlive && x.IsEscort);
                WeakestUnit = Queue.Where(x => x.IsAlive).OrderBy(x => x.Power * x.HealthRatio).FirstOrDefault();
            }

            public float HealthRatio()
            {
                var alive = Queue.Where(x => x.IsAlive).ToList();
                if (alive.Count == 0) return 0f;
                return alive.Average(x => x.HealthRatio);
            }
        }

        // Core combat state flags from the original class.
        private bool mIsCombatPrepared;
        private bool mIsCombatInitiated;
        private bool mRetreatInProgress;

        // Player ownership state: aggressor, retreating side, and winner.
        private int mAggressor = -1;
        private int mRetreatingPlayer = -1;
        private int mWinningPlayer = -1;

        // Space/land mode and circular battle-history index.
        private bool mIsSpace;
        private int mBattleID = -1;
        private int mRoundCounter;
        private AutoResolveRoundReport mLastRoundReport = new AutoResolveRoundReport();

        // The original implementation supports two participating sides.
        private readonly SideStruct[] mSides = new SideStruct[] { new SideStruct(), new SideStruct() };

        // Circular battle history buffer (MAX_HISTORY entries).
        private readonly AutoResolveBattle[] mBattleHistory = new AutoResolveBattle[MAX_HISTORY];

        public AutoResolveClass()
        {
            for (int i = 0; i < MAX_HISTORY; i++) mBattleHistory[i] = new AutoResolveBattle();
        }

        // Initializes a new auto-resolve session in space context.
        public AutoResolveHResult Prepare_For_Space()
        {
            if (mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            ResetForNewBattle(true);
            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Prepare_For_Land()
        {
            if (mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            ResetForNewBattle(false);
            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Add_Combatant(AutoResolveCombatant combatant)
        {
            if (!mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            if (mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_COMBAT_STARTED;
            if (combatant == null) return AutoResolveHResult.E_POINTER;

            int sideIndex = Owner_Enters_Fray(combatant.OwnerId);
            if (sideIndex < 0) return AutoResolveHResult.E_AUTORESOLVE_BAD_OBJECT;

            mSides[sideIndex].AddCombatant(combatant);
            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Initiate_Combat(int aggressor)
        {
            if (!mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            mAggressor = aggressor;
            mIsCombatInitiated = true;
            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Player_Retreats(int id)
        {
            if (!mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            mRetreatInProgress = true;
            mRetreatingPlayer = id;
            return AutoResolveHResult.S_OK;
        }

        // Executes one combat round: compute side force, apply contrast attacks, then attrition/retreat effects.
        public AutoResolveHResult Combat_Round(bool instant)
        {
            if (!mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;

            TargetResult resultA;
            TargetResult resultB;
            AutoResolveCombatant weakA;
            AutoResolveCombatant weakB;

            Calculate_Side_Force(mSides[0].Queue, out resultA, mSides[0].OwnerId, out weakA);
            Calculate_Side_Force(mSides[1].Queue, out resultB, mSides[1].OwnerId, out weakB);

            int winner = Determine_Winner_Index(resultA, resultB);
            int loser = winner == 0 ? 1 : 0;

            AutoResolveCombatant targetedLoser = loser == 0 ? weakA : weakB;
            bool destroyed = false;

            if (winner >= 0)
            {
                Side_Attack(mSides[winner].Queue, resultA, resultB, mSides[winner].OwnerId);
                Side_Attack(mSides[loser].Queue, resultB, resultA, mSides[loser].OwnerId);

                destroyed = Apply_Attrition(mSides[loser].Queue, ref (loser == 0 ? ref resultA : ref resultB), mSides[loser].WeakestUnit, true, loser, mSides[winner].WeakestUnit);
            }

            mRoundCounter++;
            mLastRoundReport = new AutoResolveRoundReport
            {
                RoundNumber = mRoundCounter,
                SideAUnit = Get_Lead_Unit_Name(0),
                SideBUnit = Get_Lead_Unit_Name(1),
                SideAPower = resultA.Total,
                SideBPower = resultB.Total,
                WinnerSideIndex = winner,
                WinnerOwnerId = winner >= 0 ? mSides[winner].OwnerId : -1,
                WinningUnit = winner >= 0 ? Get_Lead_Unit_Name(winner) : "(tie)",
                LosingUnit = targetedLoser != null ? targetedLoser.TypeName : "(none)",
                LosingUnitDestroyed = destroyed
            };

            if (mRetreatInProgress) Kill_Retreating_Units();

            mSides[0].UpdatePositions();
            mSides[1].UpdatePositions();

            if (mSides[0].UnitCount == 0) mWinningPlayer = mSides[1].OwnerId;
            if (mSides[1].UnitCount == 0) mWinningPlayer = mSides[0].OwnerId;

            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Cleanup_Combat()
        {
            mIsCombatPrepared = false;
            mIsCombatInitiated = false;
            mRetreatInProgress = false;
            mAggressor = -1;
            mRetreatingPlayer = -1;
            mWinningPlayer = -1;
            mRoundCounter = 0;
            mLastRoundReport = new AutoResolveRoundReport();
            mSides[0].Init();
            mSides[1].Init();
            return AutoResolveHResult.S_OK;
        }

        public int Who_Won() { return mWinningPlayer; }
        public int Get_Battle_ID() { return mBattleID; }
        public AutoResolveBattle Get_Battle(int id) { return mBattleHistory[id % MAX_HISTORY]; }
        public AutoResolveRoundReport Get_Last_Round_Report() { return mLastRoundReport; }

        public int Get_Side_A() { return mSides[0].OwnerId; }
        public int Get_Side_B() { return mSides[1].OwnerId; }
        public int Get_Side_Aggressor() { return mAggressor; }
        public int Get_Side_Defender()
        {
            if (mSides[0].OwnerId == mAggressor) return mSides[1].OwnerId;
            return mSides[0].OwnerId;
        }
        public int Side_Is_Retreating() { return mRetreatInProgress ? mRetreatingPlayer : -1; }
        public float Get_Health_Ratio(int side) { return mSides[side].HealthRatio(); }
        public int Get_Visible_Queue_Size(int side) { return mSides[side].Queue.Count(x => x.IsAlive); }

        public void Add_Tactical_Combatants() { }

        public void Kill_Retreating_Units()
        {
            int side = GetSideByOwner(mRetreatingPlayer);
            if (side < 0) return;

            foreach (var unit in mSides[side].Queue.Where(x => x.IsAlive && !x.CanRetreat).ToList())
            {
                unit.IsAlive = false;
                unit.Health = 0f;
                Update_Battle_History(unit);
            }
        }

        public bool Apply_Attrition(List<AutoResolveCombatant> units, ref TargetResult current, AutoResolveCombatant weakestUnit, bool isLoser, int index, AutoResolveCombatant killer)
        {
            var alive = units.Where(x => x.IsAlive).OrderBy(x => x.Power * x.HealthRatio).ToList();
            if (alive.Count == 0) return false;

            var victim = alive[0];
            float loss = isLoser ? 0.35f : 0.15f;
            victim.Health -= loss;
            if (victim.Health <= 0f)
            {
                victim.IsAlive = false;
                victim.Health = 0f;
                Update_Battle_History(victim);
                return true;
            }
            return false;
        }

        public bool Apply_Transport_Losses(List<AutoResolveCombatant> units, bool isPirate, AutoResolveCombatant killer)
        {
            bool killed = false;
            foreach (var unit in units.Where(x => x.IsAlive && x.IsTransport).ToList())
            {
                unit.Health -= isPirate ? 0.75f : 0.50f;
                if (unit.Health <= 0f)
                {
                    unit.IsAlive = false;
                    unit.Health = 0f;
                    killed = true;
                    Update_Battle_History(unit);
                }
            }
            return killed;
        }

        public void Find_Contrast_Index(float remainingPower, AutoResolveCombatant unit, TargetResult current, out int bestCategory)
        {
            bestCategory = 0;
            if (unit == null || unit.ContrastCategories.Count == 0) return;

            float best = float.MinValue;
            for (int i = 0; i < unit.ContrastCategories.Count; i++)
            {
                float score = current.Get(unit.ContrastCategories[i]);
                if (score > best)
                {
                    best = score;
                    bestCategory = i;
                }
            }
        }

        public void Apply_Unit_Contrast(float remainingPower, AutoResolveCombatant unit, TargetResult current, int bestCategory, List<float> factorTable, MapEnvironmentType terrain)
        {
            if (unit == null || unit.ContrastCategories.Count == 0) return;
            string key = unit.ContrastCategories[Math.Min(bestCategory, unit.ContrastCategories.Count - 1)];

            float factor = 1f;
            if (factorTable != null && factorTable.Count > bestCategory) factor = factorTable[bestCategory];

            current.Add(key, Math.Max(0f, remainingPower * factor));
        }

        public void Side_Attack(List<AutoResolveCombatant> units, TargetResult targetForce, TargetResult result, int playerId)
        {
            foreach (var unit in units.Where(x => x.IsAlive))
            {
                float remaining = unit.Power * unit.HealthRatio;
                int bestCategory;
                Find_Contrast_Index(remaining, unit, targetForce, out bestCategory);
                Apply_Unit_Contrast(remaining, unit, result, bestCategory, null, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground);
            }
        }

        public void Calculate_Side_Force(List<AutoResolveCombatant> units, out TargetResult result, int playerId, out AutoResolveCombatant weakestUnit)
        {
            result = new TargetResult();
            weakestUnit = null;

            foreach (var unit in units.Where(x => x.IsAlive))
            {
                float effectivePower = unit.Power * unit.HealthRatio;
                foreach (var category in unit.ContrastCategories)
                {
                    result.Add(category, effectivePower);
                }
            }

            weakestUnit = units.Where(x => x.IsAlive).OrderBy(x => x.Power * x.HealthRatio).FirstOrDefault();
        }

        public void Find_Special_Heroes(int index) { }

        // Appends a destroyed unit record to the current battle history slot.
        public void Update_Battle_History(AutoResolveCombatant objectUnit)
        {
            if (objectUnit == null) return;
            mBattleHistory[mBattleID].Killed.Add(new KeyValuePair<string, int>(objectUnit.TypeName, objectUnit.OwnerId));
        }

        public int Determine_Winner_Index(TargetResult resultsA, TargetResult resultsB)
        {
            float a = resultsA.Total;
            float b = resultsB.Total;
            if (Math.Abs(a - b) < 0.0001f) return 0;
            return a > b ? 0 : 1;
        }

        private string Get_Lead_Unit_Name(int side)
        {
            if (side < 0 || side >= mSides.Length) return "(none)";

            AutoResolveCombatant liveLead = mSides[side].Queue.FirstOrDefault(x => x.IsAlive && !string.IsNullOrEmpty(x.TypeName));
            if (liveLead != null) return liveLead.TypeName;

            AutoResolveCombatant anyLead = mSides[side].Queue.FirstOrDefault(x => !string.IsNullOrEmpty(x.TypeName));
            if (anyLead != null) return anyLead.TypeName;

            return "(none)";
        }

        private int Owner_Enters_Fray(int owner)
        {
            if (mSides[0].OwnerId == owner || mSides[0].OwnerId == -1)
            {
                if (mSides[0].OwnerId == -1) mSides[0].OwnerId = owner;
                return 0;
            }

            if (mSides[1].OwnerId == owner || mSides[1].OwnerId == -1)
            {
                if (mSides[1].OwnerId == -1) mSides[1].OwnerId = owner;
                return 1;
            }

            return -1;
        }

        private int GetSideByOwner(int owner)
        {
            if (mSides[0].OwnerId == owner) return 0;
            if (mSides[1].OwnerId == owner) return 1;
            return -1;
        }

        private void ResetForNewBattle(bool isSpace)
        {
            mIsCombatPrepared = true;
            mIsCombatInitiated = false;
            mRetreatInProgress = false;
            mIsSpace = isSpace;
            mRetreatingPlayer = -1;
            mWinningPlayer = -1;
            mAggressor = -1;

            mSides[0].Init();
            mSides[1].Init();
            mRoundCounter = 0;
            mLastRoundReport = new AutoResolveRoundReport();

            mBattleID = (mBattleID + 1) % MAX_HISTORY;
            mBattleHistory[mBattleID] = new AutoResolveBattle();
        }
    }

    // Simplified equivalent of TargetContrastClass::ResultType.
    public class TargetResult
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

        public float Total
        {
            get { return _values.Values.Sum(); }
        }

        public void Add(string category, float value)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            float existing;
            if (_values.TryGetValue(category, out existing)) _values[category] = existing + value;
            else _values[category] = value;
        }

        public float Get(string category)
        {
            float value;
            if (_values.TryGetValue(category, out value)) return value;
            return 0f;
        }
    }
}
