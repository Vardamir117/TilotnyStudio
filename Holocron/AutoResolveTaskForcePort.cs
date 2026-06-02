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
        E_AUTORESOLVE_BAD_OBJECT = 4,
        E_AUTORESOLVE_AGGRESSOR_ERROR = 5,
        E_AUTORESOLVE_NO_CONFLICT = 6,
        S_AUTORESOLVE_COMBAT_RETREAT = 7,
        S_AUTORESOLVE_COMBAT_OVER = 8,
        E_AUTORESOLVE_RETREATING = 9
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

        public bool ContainsNamedHero;
        public bool IsDummyGroundStructure;
        public bool IsPlanet;
        public bool HasPlanetaryBehavior;
        public bool IsSuperWeapon;
        public bool IsSuperWeaponKiller;
        public bool IsPlayableFaction = true;
        public bool IncludeGarrisonInAttrition;
        public float GarrisonPower;
        public float PlanetaryReplacementPower = -1f;
        public bool IncludePlanetTacticalBuiltObjects;
        public List<AutoResolveBuiltObject> PlanetBuiltObjects = new List<AutoResolveBuiltObject>();
        public bool AddGarrison = true;
        public bool IsDummyStarBase;

        public List<string> ContrastCategories = new List<string>();

        public float HealthRatio
        {
            get { return Math.Max(0f, Math.Min(1f, Health)); }
        }
    }

    public class AutoResolveBuiltObject
    {
        public string ContrastCategory;
        public float Power;
    }

    public class AutoResolveRoundReport
    {
        public int RoundNumber;
        public string SideAUnit;
        public int SideAUnitIndex;
        public string SideBUnit;
        public int SideBUnitIndex;
        public float SideAPower;
        public float SideBPower;
        public int WinnerSideIndex;
        public int WinnerOwnerId;
        public string WinningUnit;
        public int WinningUnitIndex;
        public string LosingUnit;
        public int LosingUnitIndex;
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
            public TargetResult TotalForce = new TargetResult();
            public bool SuperWeaponPresent;
            public bool SuperWeaponKillerPresent;

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
                TotalForce = new TargetResult();
                SuperWeaponPresent = false;
                SuperWeaponKillerPresent = false;
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
                WeakestUnit = Queue.Where(x => x.IsAlive).OrderBy(x => x.Power).FirstOrDefault();
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
        private readonly Random mAttritionRandom = new Random(1);
        private bool mBattleFought;

        public bool MidTactical { get; set; }
        public float TacticalMultiplier { get; set; } = 1.0f;

        public float LoserAttrition { get; set; } = 0.35f;
        public float WinnerAttrition { get; set; } = 0.15f;
        public float RetreatLoserAttrition { get; set; } = 0.35f;
        public float RetreatWinnerAttrition { get; set; } = 0.15f;
        public float AttritionAllowanceFactor { get; set; } = 0.333333f;
        public float TransportLosses { get; set; } = 0.5f;

        // Optional provider for PGAICommands contrast weighting: (enemyCategory, friendlyCategory) => weight.
        public Func<string, string, float> ContrastWeightProvider { get; set; }

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
            if (mIsCombatInitiated || !mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;

            mIsCombatInitiated = true;
            mAggressor = aggressor;

            // Verify that the specified aggressor is valid according to the participants.
            int aggressorSide = GetSideByOwner(aggressor);
            if (aggressorSide < 0) return AutoResolveHResult.E_AUTORESOLVE_AGGRESSOR_ERROR;

            // Verify two sides exist and each has at least one participating unit.
            for (int index = 0; index < mSides.Length; ++index)
            {
                if (mSides[index].OwnerId == -1) return AutoResolveHResult.E_AUTORESOLVE_NO_CONFLICT;
                if (!mSides[index].Queue.Any(x => x != null && x.IsAlive)) return AutoResolveHResult.E_AUTORESOLVE_NO_CONFLICT;
            }

            // C++ parity: calculate side force snapshot and special heroes on init.
            TargetResult side0Force;
            TargetResult side1Force;
            AutoResolveCombatant side0Weakest;
            AutoResolveCombatant side1Weakest;
            Calculate_Side_Force(mSides[0].Queue, out side0Force, mSides[0].OwnerId, out side0Weakest);
            Calculate_Side_Force(mSides[1].Queue, out side1Force, mSides[1].OwnerId, out side1Weakest);
            mSides[0].TotalForce = side0Force;
            mSides[1].TotalForce = side1Force;
            mSides[0].WeakestUnit = side0Weakest;
            mSides[1].WeakestUnit = side1Weakest;
            Find_Special_Heroes(0);
            Find_Special_Heroes(1);

            bool foundSuperWeapon = mSides[0].SuperWeaponPresent || mSides[1].SuperWeaponPresent;
            float side0SpaceForce = mSides[0].TotalForce.Get("__GLOBAL_SPACE__");
            float side1SpaceForce = mSides[1].TotalForce.Get("__GLOBAL_SPACE__");

            // Only transports on both sides: determine winner and force retreat immediately.
            if (!foundSuperWeapon && mIsSpace && side0SpaceForce <= 0.0f && side1SpaceForce <= 0.0f)
            {
                int loser;
                int winner;

                int side0Count = mSides[0].Queue.Count;
                int side1Count = mSides[1].Queue.Count;

                if (side0Count > side1Count)
                {
                    loser = 1;
                    winner = 0;
                }
                else if (side0Count == side1Count)
                {
                    loser = mAggressor == mSides[0].OwnerId ? 0 : 1;
                    winner = loser == 0 ? 1 : 0;
                }
                else
                {
                    loser = 0;
                    winner = 1;
                }

                mWinningPlayer = mSides[winner].OwnerId;
                Player_Retreats(mSides[loser].OwnerId);
                mBattleFought = true;
            }

            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Player_Retreats(int id)
        {
            if (!mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            if (mRetreatInProgress) return AutoResolveHResult.E_AUTORESOLVE_RETREATING;
            mRetreatInProgress = true;
            mRetreatingPlayer = id;
            return AutoResolveHResult.S_OK;
        }

        // Executes one combat round: compute side force, apply contrast attacks, then attrition/retreat effects.
        public AutoResolveHResult Combat_Round(bool instant)
        {
            if (!mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;

            if (mBattleFought)
            {
                if (mRetreatInProgress) return AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT;
                return AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER;
            }

            // C++ parity: resolve attacks against cached total force from Initiate_Combat.
            TargetResult[] results = new TargetResult[2];
            results[0] = mSides[0].TotalForce.Clone();
            results[1] = mSides[1].TotalForce.Clone();

            Side_Attack(mSides[0].Queue, mSides[1].TotalForce, results[1], mSides[0].OwnerId);
            Side_Attack(mSides[1].Queue, mSides[0].TotalForce, results[0], mSides[1].OwnerId);

            int winner = Determine_Winner_Index(results[0], results[1]);
            int loser = winner == 0 ? 1 : 0;

            AutoResolveCombatant targetedLoser = mSides[loser].WeakestUnit;
            bool destroyed = false;

            if (winner >= 0)
            {
                mWinningPlayer = mSides[winner].OwnerId;

                bool targetAliveBefore = targetedLoser != null && targetedLoser.IsAlive;

                bool piratePlayer = mSides[loser].Queue.Any(x => x != null && x.IsAlive && !x.IsPlayableFaction);
                AutoResolveCombatant winnerKiller = mSides[winner].Queue.FirstOrDefault(x => x != null && x.IsAlive);
                if (Apply_Transport_Losses(mSides[loser].Queue, piratePlayer, winnerKiller))
                {
                    Player_Retreats(mSides[loser].OwnerId);
                }

                float loserAttritionValue = mRetreatInProgress ? RetreatLoserAttrition : LoserAttrition;
                float winnerAttritionValue = mRetreatInProgress ? RetreatWinnerAttrition : WinnerAttrition;
                string globalKey = mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__";

                float loserTotal = mSides[loser].TotalForce.Get(globalKey);
                float loserResult = results[loser].Get(globalKey);
                results[loser].Set(globalKey, loserResult + ((loserTotal - loserResult) * (1.0f - loserAttritionValue)));

                float winnerTotal = mSides[winner].TotalForce.Get(globalKey);
                float winnerResult = results[winner].Get(globalKey);
                results[winner].Set(globalKey, winnerResult + ((winnerTotal - winnerResult) * (1.0f - winnerAttritionValue)));

                if (piratePlayer)
                {
                    results[loser].Set(globalKey, 0.0f);
                }

                AutoResolveCombatant wobj = mSides[winner].Queue.FirstOrDefault(x => x != null && x.IsAlive);
                AutoResolveCombatant lobj = mSides[loser].Queue.FirstOrDefault(x => x != null && x.IsAlive);

                for (int i = 0; i < mSides.Length; i++)
                {
                    bool anyLeft = Apply_Attrition(mSides[i].Queue, ref results[i], mSides[i].WeakestUnit, i == loser, i, i == loser ? wobj : lobj);
                    if (anyLeft && i == loser) Player_Retreats(mSides[i].OwnerId);
                }

                if (!mSides[loser].Queue.Any(x => x != null && x.IsAlive) && mRetreatInProgress)
                {
                    mRetreatInProgress = false;
                    mRetreatingPlayer = -1;
                }

                destroyed = targetAliveBefore && targetedLoser != null && !targetedLoser.IsAlive;
            }

            mRoundCounter++;
            mLastRoundReport = new AutoResolveRoundReport
            {
                RoundNumber = mRoundCounter,
                SideAUnit = Get_Lead_Unit_Name(0),
                SideAUnitIndex = Get_Lead_Unit_Index(0),
                SideBUnit = Get_Lead_Unit_Name(1),
                SideBUnitIndex = Get_Lead_Unit_Index(1),
                SideAPower = results[0].Total,
                SideBPower = results[1].Total,
                WinnerSideIndex = winner,
                WinnerOwnerId = winner >= 0 ? mSides[winner].OwnerId : -1,
                WinningUnit = winner >= 0 ? Get_Lead_Unit_Name(winner) : "(tie)",
                WinningUnitIndex = winner >= 0 ? Get_Lead_Unit_Index(winner) : -1,
                LosingUnit = targetedLoser != null ? targetedLoser.TypeName : "(none)",
                LosingUnitIndex = loser >= 0 ? Get_Unit_Index(loser, targetedLoser) : -1,
                LosingUnitDestroyed = destroyed
            };

            mSides[0].UpdatePositions();
            mSides[1].UpdatePositions();

            if (mSides[0].UnitCount == 0) mWinningPlayer = mSides[1].OwnerId;
            if (mSides[1].UnitCount == 0) mWinningPlayer = mSides[0].OwnerId;

            mBattleFought = true;
            if (mRetreatInProgress) return AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT;
            return AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER;
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
            mBattleFought = false;
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
        public int Get_Visible_Queue_Size(int side)
        {
            // C++ currently returns a fixed 3 with a TODO for true visibility logic.
            side = side;
            return 3;
        }

        public void Add_Tactical_Combatants() { }

        public void Kill_Retreating_Units()
        {
            if (mRetreatingPlayer == -1) return;

            int winner = mSides[0].OwnerId == mRetreatingPlayer ? 1 : 0;
            int loser = mSides[1].OwnerId == mRetreatingPlayer ? 1 : 0;
            if (loser == winner) return;

            if (mSides[winner].Queue.Count == 0) return;

            foreach (AutoResolveCombatant unit in mSides[loser].Queue.Where(x => x != null && x.IsAlive).ToList())
            {
                unit.IsAlive = false;
                Update_Battle_History(unit);
            }

            mRetreatInProgress = false;
            mRetreatingPlayer = -1;
        }

        public bool Apply_Attrition(List<AutoResolveCombatant> units, ref TargetResult current, AutoResolveCombatant weakestUnit, bool isLoser, int index, AutoResolveCombatant killer)
        {
            float attritionAllowanceFactor = Math.Max(0f, AttritionAllowanceFactor);
            bool anySurvivors = false;
            bool weakKilled = false;

            // MLL: Hack to make tactical auto resolve less efficient.
            if (MidTactical) current.MultiplyAll(Math.Max(0f, TacticalMultiplier));

            // Mirrors C++ static left_overs container for survivors retained after attrition.
            List<AutoResolveCombatant> leftOvers = new List<AutoResolveCombatant>();
            List<AutoResolveCombatant> working = units.Where(x => x != null && x.IsAlive).ToList();

            while (working.Count != 0)
            {
                int unitIndex = -1;
                bool isStructure = false;

                // Prioritize named heroes, then dummy ground structures, then random fallback.
                for (int i = 0; i < working.Count; i++)
                {
                    AutoResolveCombatant candidate = working[i];
                    if (candidate.ContainsNamedHero)
                    {
                        unitIndex = i;
                        break;
                    }
                    if (candidate.IsDummyGroundStructure)
                    {
                        unitIndex = i;
                        isStructure = true;
                        break;
                    }
                }

                if (unitIndex < 0) unitIndex = mAttritionRandom.Next(0, working.Count);

                AutoResolveCombatant unit = working[unitIndex];
                bool killUnit = false;
                bool killBase = false;

                if (!mIsSpace || !unit.IsTransport)
                {
                    if (isLoser && !unit.IsPlayableFaction && !unit.IsPlanet)
                    {
                        // Non-playable factions are wiped out when they lose.
                        killUnit = true;
                    }
                    else if (unit.IsSuperWeapon)
                    {
                        killUnit = false;
                        if (isLoser && SideHasSuperWeaponKiller(index == 0 ? 1 : 0))
                        {
                            killUnit = true;
                            if (ReferenceEquals(weakestUnit, unit)) weakestUnit = null;
                        }
                    }
                    else if (isLoser && unit.IsPlanet)
                    {
                        // Loser base is always completely destroyed.
                        // Placeholder: starbase/special-structure cleanup requires full game object model.
                        Update_Battle_History(unit);
                        killBase = true;
                        killUnit = false;
                    }
                    else if (isLoser && isStructure)
                    {
                        // MLL: Kill all structures if the player is the loser.
                        killUnit = true;
                    }
                    else
                    {
                        killUnit = true;

                        // Apply garrison units (placeholder-backed by precomputed GarrisonPower).
                        if (unit.IncludeGarrisonInAttrition && unit.GarrisonPower > 0f)
                        {
                            current.ReduceTotalBy(unit.GarrisonPower);
                        }

                        if (unit.HasPlanetaryBehavior)
                        {
                            // Possibly reduce level of star base.
                            // Placeholder: PlanetaryReplacementPower emulates new_type->Get_AI_Combat_Power_Metric().
                            if (mIsSpace)
                            {
                                if (unit.PlanetaryReplacementPower >= 0f)
                                {
                                    current.ReduceTotalBy(unit.PlanetaryReplacementPower);
                                }
                                else
                                {
                                    killBase = true;
                                }
                            }
                            killUnit = false;
                        }
                        else if (current.Total - (unit.Power * attritionAllowanceFactor) > 0.0f)
                        {
                            current.ReduceTotalBy(unit.Power);
                            killUnit = false;
                        }
                    }

                    // Placeholder: AI learning system Register_Unit_Survival branch is not available in this port.
                }

                if (killUnit)
                {
                    if (!ReferenceEquals(weakestUnit, unit))
                    {
                        // MLL: Don't allow survivors if auto resolving.
                        // Placeholder: survivor-upon-death flag requires full object pointer model.
                        unit.IsAlive = false;
                        Update_Battle_History(unit);
                    }
                    else
                    {
                        weakKilled = true;
                    }
                }
                else if (!killBase)
                {
                    leftOvers.Add(unit);
                }

                working.RemoveAt(unitIndex);
            }

            units.Clear();
            units.AddRange(leftOvers);
            anySurvivors = leftOvers.Count != 0;

            if (anySurvivors && weakestUnit != null && weakKilled)
            {
                // MLL: Don't allow survivors if auto resolving.
                weakestUnit.IsAlive = false;
                Update_Battle_History(weakestUnit);
            }

            if (!anySurvivors && !isLoser && weakestUnit != null)
            {
                units.Add(weakestUnit);
                return true;
            }

            return anySurvivors;
        }

        public bool Apply_Transport_Losses(List<AutoResolveCombatant> units, bool isPirate, AutoResolveCombatant killer)
        {
            float transportLosses = Math.Max(0f, Math.Min(1f, TransportLosses));

            if (!mIsSpace) return false;

            int tcnt = 0;
            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                if (unit != null && unit.IsAlive && unit.IsTransport) tcnt++;
            }

            int rcnt = tcnt == 1 ? 0 : (int)(((float)tcnt) * (1.0f - transportLosses) + 0.5f);
            if (isPirate) rcnt = 0;

            tcnt = 0;
            bool anyLeft = false;

            for (int i = 0; i < units.Count && tcnt < rcnt; i++)
            {
                AutoResolveCombatant unit = units[i];
                if (unit != null && unit.IsAlive && unit.IsTransport && unit.ContainsNamedHero)
                {
                    ++tcnt;
                    units.RemoveAt(i);
                    --i;
                    anyLeft = true;
                }
            }

            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                if (unit == null || !unit.IsAlive || !unit.IsTransport) continue;

                if (tcnt >= rcnt || isPirate)
                {
                    Update_Battle_History(unit);
                    unit.IsAlive = false;
                }
                else
                {
                    anyLeft = true;
                }

                tcnt++;
            }

            return anyLeft;
        }

        public void Find_Contrast_Index(float remainingPower, AutoResolveCombatant unit, TargetResult current, out string bestCategory)
        {
            bestCategory = null;
            if (unit == null || unit.ContrastCategories == null || unit.ContrastCategories.Count == 0) return;

            float bestWeight = 0.0f;
            foreach (KeyValuePair<string, float> kv in current.GetEntries())
            {
                string enemyCategory = kv.Key;
                if (string.IsNullOrWhiteSpace(enemyCategory) || enemyCategory.StartsWith("__GLOBAL_", StringComparison.OrdinalIgnoreCase)) continue;

                float remaining = kv.Value;
                if (remaining <= 0.0f) continue;

                float contrastWeight = Get_Unit_Contrast_Weight(unit, enemyCategory);
                if (contrastWeight <= 0.0f) continue;

                float denominator = Math.Max(remaining, remainingPower * contrastWeight);
                if (denominator <= 0.0f) continue;

                float buildWeight = (remaining - remainingPower * contrastWeight) / denominator;
                buildWeight *= -buildWeight;
                buildWeight += 1.0f;
                buildWeight = Math.Max(buildWeight, 0.0f);
                buildWeight *= contrastWeight;

                if (buildWeight > bestWeight)
                {
                    bestWeight = buildWeight;
                    bestCategory = enemyCategory;
                }
            }
        }

        public void Apply_Unit_Contrast(float remainingPower, AutoResolveCombatant unit, TargetResult current, string bestCategory, List<float> factorTable, MapEnvironmentType terrain)
        {
            if (unit == null) return;

            float factor = 1f;
            if (factorTable != null && factorTable.Count > 0) factor = factorTable[0];

            float appliedPower = Math.Max(0f, remainingPower * Math.Max(0f, factor));
            string globalKey = mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__";

            if (string.IsNullOrWhiteSpace(bestCategory))
            {
                current.Reduce(globalKey, appliedPower);
                return;
            }

            float contrastWeight = Get_Unit_Contrast_Weight(unit, bestCategory);
            appliedPower *= Math.Max(0f, contrastWeight);

            float available = current.Get(bestCategory);
            float categoryApplied = Math.Min(Math.Max(0f, available), appliedPower);
            if (categoryApplied > 0f)
            {
                current.Reduce(bestCategory, categoryApplied);
            }

            // Keep global force in sync with applied pressure.
            current.Reduce(globalKey, appliedPower);
        }

        public void Side_Attack(List<AutoResolveCombatant> units, TargetResult targetForce, TargetResult result, int playerId)
        {
            foreach (AutoResolveCombatant unit in units.Where(x => x.IsAlive))
            {
                float remaining = unit.Power;
                string bestCategory;
                Find_Contrast_Index(remaining, unit, targetForce, out bestCategory);
                Apply_Unit_Contrast(remaining, unit, result, bestCategory, null, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground);
            }
        }

        public void Calculate_Side_Force(List<AutoResolveCombatant> units, out TargetResult result, int playerId, out AutoResolveCombatant weakestUnit)
        {
            // C++ parity: result has global ground/space buckets plus contrast-type buckets.
            // Placeholder: TargetContrastClass::Init_Contrast_Type_List and typed result slots are represented by category keys.
            result = new TargetResult();
            weakestUnit = null;

            float weakestVal = float.MaxValue;

            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                if (unit == null || !unit.IsAlive) continue;

                if (!mIsSpace && unit.IsPlanet)
                {
                    // C++: use persistent tactical built objects to compute ground base strength.
                    // Placeholder: PlanetBuiltObjects carries pre-projected built tactical object category/power pairs.
                    if (unit.IncludePlanetTacticalBuiltObjects)
                    {
                        foreach (AutoResolveBuiltObject built in unit.PlanetBuiltObjects)
                        {
                            if (built == null || built.Power <= 0f || string.IsNullOrWhiteSpace(built.ContrastCategory)) continue;
                            result.Add(built.ContrastCategory, built.Power);
                            result.Add("__GLOBAL_GROUND__", built.Power);
                        }
                    }
                    continue;
                }

                // C++: Apply garrison units unless in suppressed dummy-starbase mid-tactical path.
                // Placeholder: GarrisonPower is pre-aggregated (type count * starting count * AI power metric).
                bool addGarrison = unit.AddGarrison;
                if (addGarrison && unit.IsDummyStarBase && MidTactical) addGarrison = false;
                if (addGarrison && unit.GarrisonPower > 0f)
                {
                    string garrisonCategory = unit.ContrastCategories.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(garrisonCategory)) result.Add(garrisonCategory, unit.GarrisonPower);
                    result.Add(mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__", unit.GarrisonPower);
                }

                // C++: transports do not contribute to direct side force in space mode.
                if (unit.IsTransport && mIsSpace) continue;

                if (unit.Power < weakestVal)
                {
                    weakestUnit = unit;
                    weakestVal = unit.Power;
                }

                // C++: choose first matching contrast category and add unit AI combat power metric.
                // Placeholder: category mask iteration is represented by first defined category.
                string unitCategory = unit.ContrastCategories.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(unitCategory)) result.Add(unitCategory, unit.Power);
                result.Add(mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__", unit.Power);
            }
        }

        public void Find_Special_Heroes(int index)
        {
            if (index < 0 || index >= mSides.Length) return;

            SideStruct side = mSides[index];
            side.SuperWeaponPresent = false;
            side.SuperWeaponKillerPresent = false;

            foreach (AutoResolveCombatant combatant in side.Queue)
            {
                if (combatant == null || !combatant.IsAlive) continue;
                if (combatant.IsSuperWeapon) side.SuperWeaponPresent = true;
                if (combatant.IsSuperWeaponKiller) side.SuperWeaponKillerPresent = true;
            }
        }

        // Appends a destroyed unit record to the current battle history slot.
        public void Update_Battle_History(AutoResolveCombatant objectUnit)
        {
            if (objectUnit == null) return;
            mBattleHistory[mBattleID].Killed.Add(new KeyValuePair<string, int>(objectUnit.TypeName, objectUnit.OwnerId));
        }

        public int Determine_Winner_Index(TargetResult resultsA, TargetResult resultsB)
        {
            // Death Star hack. Super weapon always wins unless opposition has a killer.
            if (mSides[0].SuperWeaponPresent && !mSides[1].SuperWeaponKillerPresent)
            {
                Player_Retreats(mSides[1].OwnerId);
            }
            else if (mSides[1].SuperWeaponPresent && !mSides[0].SuperWeaponKillerPresent)
            {
                Player_Retreats(mSides[0].OwnerId);
            }

            if (mRetreatInProgress)
            {
                return mRetreatingPlayer == mSides[0].OwnerId ? 1 : 0;
            }

            float totalA = 0.0f;
            bool anyPositiveA = false;
            foreach (float force in resultsA.GetPositiveForces())
            {
                if (force > 0.0f)
                {
                    anyPositiveA = true;
                    totalA += force;
                }
            }

            float totalB = 0.0f;
            bool anyPositiveB = false;
            foreach (float force in resultsB.GetPositiveForces())
            {
                if (force > 0.0f)
                {
                    anyPositiveB = true;
                    totalB += force;
                }
            }

            if (anyPositiveA && anyPositiveB)
            {
                // Placeholder: human-vs-ai and playable-faction tie-breaks require full PlayerClass/Faction data.
                return totalA > totalB ? 0 : 1;
            }
            else if ((anyPositiveA || anyPositiveB) && Math.Abs(totalA - totalB) > 0.0001f)
            {
                return totalA > totalB ? 0 : 1;
            }
            else
            {
                return mAggressor == mSides[0].OwnerId ? 0 : 1;
            }
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

        private int Get_Lead_Unit_Index(int side)
        {
            if (side < 0 || side >= mSides.Length) return -1;

            for (int i = 0; i < mSides[side].Queue.Count; i++)
            {
                AutoResolveCombatant unit = mSides[side].Queue[i];
                if (unit.IsAlive && !string.IsNullOrEmpty(unit.TypeName)) return i;
            }

            for (int i = 0; i < mSides[side].Queue.Count; i++)
            {
                AutoResolveCombatant unit = mSides[side].Queue[i];
                if (!string.IsNullOrEmpty(unit.TypeName)) return i;
            }

            return -1;
        }

        private int Get_Unit_Index(int side, AutoResolveCombatant combatant)
        {
            if (side < 0 || side >= mSides.Length || combatant == null) return -1;
            return mSides[side].Queue.IndexOf(combatant);
        }

        private bool SideHasSuperWeaponKiller(int side)
        {
            if (side < 0 || side >= mSides.Length) return false;
            return mSides[side].Queue.Any(x => x != null && x.IsAlive && x.IsSuperWeaponKiller);
        }

        private float Get_Unit_Contrast_Weight(AutoResolveCombatant unit, string enemyCategory)
        {
            if (unit == null) return 1.0f;
            if (unit.ContrastCategories == null || unit.ContrastCategories.Count == 0) return 1.0f;
            if (string.IsNullOrWhiteSpace(enemyCategory)) return 1.0f;

            float best = 0.0f;
            for (int i = 0; i < unit.ContrastCategories.Count; i++)
            {
                string friendlyCategory = unit.ContrastCategories[i];
                if (string.IsNullOrWhiteSpace(friendlyCategory)) continue;

                float weight = 1.0f;
                if (ContrastWeightProvider != null)
                {
                    weight = ContrastWeightProvider(enemyCategory, friendlyCategory);
                }

                if (weight > best) best = weight;
            }

            return best > 0.0f ? best : 1.0f;
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

            mSides[0].Init();
            mSides[1].Init();
            mRoundCounter = 0;
            mLastRoundReport = new AutoResolveRoundReport();
            mBattleFought = false;

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

        public void Reduce(string category, float amount)
        {
            if (string.IsNullOrWhiteSpace(category) || amount <= 0f) return;
            float existing;
            if (!_values.TryGetValue(category, out existing)) return;
            _values[category] = Math.Max(0f, existing - amount);
        }

        public void Set(string category, float value)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            _values[category] = value;
        }

        public float Get(string category)
        {
            float value;
            if (_values.TryGetValue(category, out value)) return value;
            return 0f;
        }

        public IEnumerable<float> GetPositiveForces()
        {
            return _values.Values.Where(x => x > 0.0f);
        }

        public IEnumerable<KeyValuePair<string, float>> GetEntries()
        {
            return _values;
        }

        public TargetResult Clone()
        {
            TargetResult copy = new TargetResult();
            foreach (KeyValuePair<string, float> kv in _values)
            {
                copy.Set(kv.Key, kv.Value);
            }
            return copy;
        }

        public void ReduceTotalBy(float amount)
        {
            if (amount <= 0f || _values.Count == 0) return;

            float remaining = amount;
            foreach (string key in _values.OrderByDescending(x => x.Value).Select(x => x.Key).ToList())
            {
                if (remaining <= 0f) break;

                float value = _values[key];
                float delta = Math.Min(value, remaining);
                _values[key] = value - delta;
                remaining -= delta;
            }
        }

        public void MultiplyAll(float factor)
        {
            if (factor < 0f) factor = 0f;

            foreach (string key in _values.Keys.ToList())
            {
                _values[key] = _values[key] * factor;
            }
        }
    }
}
