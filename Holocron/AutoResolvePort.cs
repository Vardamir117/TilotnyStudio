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
        public float Power = 1.0f;

        public bool ContainsNamedHero;
        public bool IsDummyGroundStructure;
        public bool IsPlanet;
        public bool HasPlanetaryBehavior;
        public bool IsSuperWeapon;
        public bool IsSuperWeaponKiller;
        public bool IsPlayableFaction = true;
        public float GarrisonPower;
        public List<AutoResolveBuiltObject> GarrisonEntries = new List<AutoResolveBuiltObject>();
        public float PlanetaryReplacementPower = -1f;
        public bool IncludePlanetTacticalBuiltObjects;
        public List<AutoResolveBuiltObject> PlanetBuiltObjects = new List<AutoResolveBuiltObject>();
        public bool AddGarrison = false; // (object->Get_Parent_Mode_ID() == INVALID_OBJECT_ID) is the C code
        public bool IsDummyStarBase;

        // Placeholder for C++ hero special ability strength factors (category -> multiplier).
        public bool HasSpecialAbility;
        public Dictionary<string, float> SpecialAbilityUnitStrengthFactors = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public ulong CategoryMask;
    }

    public class AutoResolveBuiltObject
    {
        public string ContrastCategory;
        public ulong CategoryMask;
        public float Power;
    }

    public class AutoResolveEngagementReport
    {
        public int AttackerOwnerId;
        public string SourceTypeName;
        public int SourceUnitIndex;
        public string SourceKind;
        public string SourceCategory;
        public float SourcePowerBefore;
        public float SourcePowerAfter;
        public float ScaledPower;
        public string TargetCategory;
        public float TargetCategoryBefore;
        public float TargetCategoryAfter;
        public float TargetGlobalBefore;
        public float TargetGlobalAfter;
        public float HeroMultiplier;
        public float ContrastMultiplier;
        public float TotalMultiplier;
        public float AppliedCombatPower;
    }

    public class AutoResolveAttritionReport
    {
        public int SideOwnerId;
        public int SideIndex;
        public bool IsLoserSide;
        public string UnitTypeName;
        public float UnitPower;
        public float ForceBefore;
        public float ForceAfter;
        public string Decision;
        public string Notes;
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
        private readonly List<AutoResolveEngagementReport> mLastEngagements = new List<AutoResolveEngagementReport>();
        private readonly List<AutoResolveAttritionReport> mLastAttritionReports = new List<AutoResolveAttritionReport>();
        private readonly Random mAttritionRandom = new Random(1);
        private bool mBattleFought;
        private string mLastWinnerDecision = "(not evaluated)";

        public bool MidTactical { get; set; }
        public float TacticalMultiplier { get; set; } = 1.0f;

        public float LoserAttrition { get; set; } = 0.35f;
        public float WinnerAttrition { get; set; } = 0.15f;
        public float RetreatLoserAttrition { get; set; } = 0.35f;
        public float RetreatWinnerAttrition { get; set; } = 0.15f;
        public float AttritionAllowanceFactor { get; set; } = 0.333333f;
        public float TransportLosses { get; set; } = 0.333333f;

        // Provider for PGAICommands weighted contrast entries by enemy category mask.
        public Func<ulong, List<TargetContrastPort.WeightedCategoryEntry>> ContrastWeightProvider { get; set; }
        public Func<string, ulong> CategoryMaskProvider { get; set; }
        public Func<ulong, string> CategoryNameProvider { get; set; }

        // The original implementation supports two participating sides.
        private readonly SideStruct[] mSides = new SideStruct[] { new SideStruct(), new SideStruct() };

        // Circular battle history buffer (MAX_HISTORY entries).
        private readonly AutoResolveBattle[] mBattleHistory = new AutoResolveBattle[MAX_HISTORY];

        // C++-style deterministic contrast index layout for ResultType access by index.
        private const int GLOBAL_GROUND_INDEX = 0;
        private const int GLOBAL_SPACE_INDEX = 1;
        private const ulong GLOBAL_GROUND_MASK = 0UL;
        private const ulong GLOBAL_SPACE_MASK = 0UL;
        private readonly Dictionary<ulong, int> mContrastCategoryToIndex = new Dictionary<ulong, int>();
        private readonly List<ulong> mContrastIndexToCategory = new List<ulong>();
        private readonly Dictionary<ulong, string> mContrastCategoryDisplayName = new Dictionary<ulong, string>();

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

            Build_Contrast_Index_Map();

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
            float side0SpaceForce = mSides[0].TotalForce[GLOBAL_SPACE_INDEX].Force;
            float side1SpaceForce = mSides[1].TotalForce[GLOBAL_SPACE_INDEX].Force;

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
            if (!mIsCombatInitiated)
            {
                return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            }

            if (!mBattleFought)
            {
                float loserAttritionValue = mRetreatInProgress ? RetreatLoserAttrition : LoserAttrition;
                float winnerAttritionValue = mRetreatInProgress ? RetreatWinnerAttrition : WinnerAttrition;

                TargetResult[] results = new TargetResult[2];
                mLastEngagements.Clear();
                mLastAttritionReports.Clear();
                Side_Attack(mSides[0].Queue, ref mSides[1].TotalForce, ref results[1], mSides[0].OwnerId);
                Side_Attack(mSides[1].Queue, ref mSides[0].TotalForce, ref results[0], mSides[1].OwnerId);

                int winner = Determine_Winner_Index(results[0], results[1]);
                int loser = (winner == 0 ? 1 : 0);

                bool piratePlayer = false;
                if (mSides[loser].Queue.Any(x => x != null && x.IsAlive && !x.IsPlayableFaction))
                {
                    piratePlayer = true;
                }

                mWinningPlayer = mSides[winner].OwnerId;

                AutoResolveCombatant wobj = mSides[winner].Queue[0];
                AutoResolveCombatant lobj = mSides[loser].Queue[0];

                if (Apply_Transport_Losses(mSides[loser].Queue, piratePlayer, wobj))
                {
                    Player_Retreats(mSides[loser].OwnerId);
                }

                int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                results[loser][globalIndex].Force += ((mSides[loser].TotalForce[globalIndex].Force - results[loser][globalIndex].Force) *
                    (1.0f - loserAttritionValue));

                results[winner][globalIndex].Force += ((mSides[winner].TotalForce[globalIndex].Force - results[winner][globalIndex].Force) *
                    (1.0f - winnerAttritionValue));

                mSides[loser].WeakestUnit = null;
                if (piratePlayer)
                {
                    results[loser][globalIndex].Force = 0.0f;
                }

                for (int i = 0; i < mSides.Length; i++)
                {
                    bool anyLeft = Apply_Attrition(mSides[i].Queue, ref results[i], mSides[i].WeakestUnit, i == loser, i, i == loser ? wobj : lobj);

                    if (anyLeft && i == loser)
                    {
                        Player_Retreats(mSides[i].OwnerId);
                    }
                }

                if (mSides[loser].Queue.Count == 0 && mRetreatInProgress)
                {
                    mRetreatInProgress = false;
                    mRetreatingPlayer = -1;
                }

                mBattleFought = true;
            }

            if (mRetreatInProgress)
            {
                return AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT;
            }
            else
            {
                return AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER;
            }
        }

        public AutoResolveHResult Cleanup_Combat()
        {
            mIsCombatPrepared = false;
            mIsCombatInitiated = false;
            mRetreatInProgress = false;
            mAggressor = -1;
            mRetreatingPlayer = -1;
            mWinningPlayer = -1;
            mLastEngagements.Clear();
            mLastAttritionReports.Clear();
            mBattleFought = false;
            mLastWinnerDecision = "(not evaluated)";
            mSides[0].Init();
            mSides[1].Init();
            return AutoResolveHResult.S_OK;
        }

        public int Who_Won() { return mWinningPlayer; }
        public string Get_Last_Winner_Decision() { return mLastWinnerDecision; }
        public List<AutoResolveEngagementReport> Get_Last_Engagements() { return new List<AutoResolveEngagementReport>(mLastEngagements); }
        public List<AutoResolveAttritionReport> Get_Last_Attrition_Reports() { return new List<AutoResolveAttritionReport>(mLastAttritionReports); }
        public int Side_Is_Retreating() { return mRetreatInProgress ? mRetreatingPlayer : -1; }
        public AutoResolveBattle Get_Current_Battle_History()
        {
            AutoResolveBattle battle = new AutoResolveBattle();
            battle.Planet = mBattleHistory[mBattleID].Planet;
            battle.Killed.AddRange(mBattleHistory[mBattleID].Killed);
            return battle;
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
            float attritionAllowanceFactor = AttritionAllowanceFactor;
            bool anySurvivors = false;
            bool weakKilled = false;

            int global_index = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
            int sideOwnerId = (index >= 0 && index < mSides.Length) ? mSides[index].OwnerId : -1;

            // MLL: Hack to make tactical auto resolve less efficient.
            if (MidTactical)
            {
                current[global_index].Force *= TacticalMultiplier;
            }

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

                // logging info
                AutoResolveAttritionReport attritionReport = new AutoResolveAttritionReport();
                attritionReport.SideOwnerId = sideOwnerId;
                attritionReport.SideIndex = index;
                attritionReport.IsLoserSide = isLoser;
                attritionReport.UnitTypeName = unit.TypeName;
                attritionReport.UnitPower = unit.Power;
                float currentTotal = 0f;
                for (int t = 0; t < current.Count; t++) currentTotal += current[t].Force;
                attritionReport.ForceBefore = currentTotal;
                attritionReport.Decision = "Undecided";
                attritionReport.Notes = "";

                if (!mIsSpace || !unit.IsTransport)
                {
                    if (isLoser && !unit.IsPlayableFaction && !unit.IsPlanet)
                    {
                        // Non-playable factions are wiped out when they lose.
                        killUnit = true;
                        // logging info
                        attritionReport.Decision = "KillUnit";
                        attritionReport.Notes = "Non-playable losing faction unit is always destroyed.";
                    }
                    else if (unit.IsSuperWeapon)
                    {
                        killUnit = false;
                        //logging ingo
                        attritionReport.Decision = "KeepUnit";
                        attritionReport.Notes = "Super weapon survives by default.";
                        if (isLoser && SideHasSuperWeaponKiller(index == 0 ? 1 : 0))
                        {
                            killUnit = true;
                            // logging info
                            attritionReport.Decision = "KillUnit";
                            attritionReport.Notes = "Super weapon killed by opposing super-weapon killer.";
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
                        //logging info
                        attritionReport.Decision = "KillBase";
                        attritionReport.Notes = "Losing planet/base branch destroys base state.";
                    }
                    else if (isLoser && isStructure)
                    {
                        // MLL: Kill all structures if the player is the loser.
                        killUnit = true;
                        //logging info
                        attritionReport.Decision = "KillUnit";
                        attritionReport.Notes = "Losing structure is always destroyed.";
                    }
                    else
                    {
                        killUnit = true;

                        // Apply garrison units.
                        if (unit.AddGarrison && unit.GarrisonPower > 0f)
                        {
                            current[global_index].Force -= unit.GarrisonPower;
                            current[global_index].Force = Math.Max(current[global_index].Force, 0.0f);
                            // logging info
                            attritionReport.Notes = "Applied garrison power reduction of " + unit.GarrisonPower.ToString("0.###") + ".";
                        }

                        if (unit.HasPlanetaryBehavior)
                        {
                            // Possibly reduce level of star base.
                            // Placeholder: PlanetaryReplacementPower emulates new_type->Get_AI_Combat_Power_Metric().
                            if (mIsSpace)
                            {
                                if (unit.PlanetaryReplacementPower >= 0f)
                                {
                                    current[global_index].Force -= unit.PlanetaryReplacementPower;
                                    //logging info
                                    attritionReport.Decision = "KeepUnit";
                                    attritionReport.Notes = "Planetary replacement base retained with replacement power " + unit.PlanetaryReplacementPower.ToString("0.###") + ".";
                                }
                                else
                                {
                                    killBase = true;
                                    //logging info
                                    attritionReport.Decision = "KillBase";
                                    attritionReport.Notes = "No planetary replacement base available.";
                                }
                            }
                            killUnit = false;
                        }
                        else if (current[global_index].Force - (unit.Power * attritionAllowanceFactor) > 0.0f)
                        {
                                current[global_index].Force -= unit.Power;
                                current[global_index].Force = Math.Max(current[global_index].Force, 0.0f);
                                killUnit = false;

                                // logging info
                                attritionReport.Decision = "KeepUnit";
                                attritionReport.Notes = "Unit survives by attrition allowance check.";
                        }
                    }

                    // Placeholder: AI learning system Register_Unit_Survival branch is not available in this port.
                }

                // logging info: only mark KillUnit here if no branch already decided the outcome.
                if (attritionReport.Decision == "Undecided")
                {
                    attritionReport.Decision = "KillUnit";
                    if (string.IsNullOrEmpty(attritionReport.Notes))
                    {
                        attritionReport.Notes = "Insufficient remaining force after attrition allowance check.";
                    }
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
                        // logging info
                        attritionReport.Notes += (string.IsNullOrEmpty(attritionReport.Notes) ? "" : " ") + "Weakest unit deferred for post-loop kill handling.";
                    }
                }
                else if (!killBase)
                {
                    leftOvers.Add(unit);
                }

                currentTotal = 0f;
                for (int t = 0; t < current.Count; t++) currentTotal += current[t].Force;
                // logging info
                attritionReport.ForceAfter = currentTotal;
                mLastAttritionReports.Add(attritionReport);

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

                // logging info
                AutoResolveAttritionReport weakestReport = new AutoResolveAttritionReport();
                weakestReport.SideOwnerId = sideOwnerId;
                weakestReport.SideIndex = index;
                weakestReport.IsLoserSide = isLoser;
                weakestReport.UnitTypeName = weakestUnit.TypeName;
                weakestReport.UnitPower = weakestUnit.Power;
                float weakestTotal = 0f;
                for (int t = 0; t < current.Count; t++) weakestTotal += current[t].Force;
                weakestReport.ForceBefore = weakestTotal;
                weakestReport.ForceAfter = weakestTotal;
                weakestReport.Decision = "KillUnit";
                weakestReport.Notes = "Deferred weakest-unit kill applied after survivor pass.";
                mLastAttritionReports.Add(weakestReport);
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
            float transportLosses = TransportLosses;

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

        public void Find_Contrast_Index(float remainingPower, ulong categoryMask, TargetResult current, out int bestCategoryIndex)
        {
            bestCategoryIndex = -1;

            float bestWeight = 0.0f;
            for (int i = 0; i < current.Count; i++)
            {
                ulong enemyCategory = current[i].Category;
                if (enemyCategory == 0UL) continue;

                float remaining = current[i].Force;
                if (remaining <= 0.0f) continue;

                float contrastWeight = TargetContrastPort.Get_Average_Contrast_Factor(categoryMask, ContrastWeightProvider(enemyCategory));
                if (contrastWeight <= 0.0f) continue;

                float denominator = Math.Max(remaining, remainingPower * contrastWeight);
                float buildWeight = (remaining - remainingPower * contrastWeight) / denominator;
                buildWeight *= -buildWeight;
                buildWeight += 1.0f;
                buildWeight = Math.Max(buildWeight, 0.0f);
                buildWeight *= contrastWeight;

                if (buildWeight > bestWeight)
                {
                    bestWeight = buildWeight;
                    bestCategoryIndex = i;
                }
            }
        }

        public void Apply_Unit_Contrast(ref float remainingPower, ulong categoryMask, ref TargetResult current, int bestCategoryIndex, List<float> factorTable, MapEnvironmentType terrain, AutoResolveEngagementReport engagement)
        {
            float originalPower = remainingPower;
            float factor = 0.0f;
            float heroMultiplier = 1.0f;
            float contrastMultiplier = 1.0f;

            ulong ctype = categoryMask;
            int cval = Get_First_Bit_Set(ctype);

            while (cval > -1)
            {
                if (factorTable[cval] > factor)
                {
                    factor = factorTable[cval] + 1.0f;
                }

                ctype &= ~(1UL << cval);
                cval = Get_First_Bit_Set(ctype);
            }

            if (factor != 0.0f)
            {
                remainingPower *= factor;
                heroMultiplier = factor;
            }

            if (bestCategoryIndex > 0)
            {
                ulong targetMask = current[bestCategoryIndex].Category;
                float contrastWeight = TargetContrastPort.Get_Average_Contrast_Factor(categoryMask, ContrastWeightProvider(targetMask));
                // C++ code has a terrain effectiveness scaling here, but nobody uses it
                remainingPower *= contrastWeight;
                contrastMultiplier = contrastWeight;
                engagement.ScaledPower = remainingPower;

                if (string.IsNullOrWhiteSpace(engagement.SourceCategory))
                {
                    engagement.SourceCategory = Get_Best_Source_Category_For_Target(categoryMask, targetMask);
                }

                float modifiedForceApplied = Math.Min(current[bestCategoryIndex].Force, remainingPower);
                remainingPower = (1.0f - modifiedForceApplied / remainingPower) * originalPower;
                current[bestCategoryIndex].Force -= modifiedForceApplied;

                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].Category == 0UL && current[i].Ground == current[bestCategoryIndex].Ground)
                    {
                        current[i].Force -= modifiedForceApplied + remainingPower;
                        break;
                    }
                }

                engagement.AppliedCombatPower = modifiedForceApplied;
            }
            else
            {
                bool ground = !mIsSpace;
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].Category == 0UL && current[i].Ground == ground)
                    {
                        current[i].Force -= remainingPower;
                        break;
                    }
                }

                engagement.ScaledPower = remainingPower;
                engagement.SourceCategory = Get_Best_Source_Category_For_Target(categoryMask, 0UL);
                engagement.AppliedCombatPower = remainingPower;
                remainingPower = 0.0f;
            }

            engagement.HeroMultiplier = heroMultiplier;
            engagement.ContrastMultiplier = contrastMultiplier;
            engagement.TotalMultiplier = heroMultiplier * contrastMultiplier;
            engagement.SourcePowerAfter = remainingPower;
        }
           
        // logging util
        private string Get_Best_Source_Category_For_Target(ulong unitCategoryMask, ulong targetMask)
        {
            string fallback = Get_Display_Category_Name(unitCategoryMask);
            string bestCategory = fallback;
            float bestWeight = float.MinValue;

            for (int bit = 0; bit < 64; bit++)
            {
                ulong friendlyMask = 1UL << bit;
                if ((unitCategoryMask & friendlyMask) == 0UL) continue;

                float weight = TargetContrastPort.Get_Average_Contrast_Factor(
                    friendlyMask,
                    ContrastWeightProvider(targetMask));

                if (Math.Abs(weight - 1.0f) <= 0.0001f) continue;

                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    bestCategory = Get_Display_Category_Name(friendlyMask);
                }
            }

            return bestCategory;
        }

        public void Side_Attack(List<AutoResolveCombatant> units, ref TargetResult targetForce, ref TargetResult result, int playerId)
        {
            result = targetForce.Clone();
            result[GLOBAL_GROUND_INDEX].Ground = true;
            result[GLOBAL_SPACE_INDEX].Ground = false;

            List<float> catTable = new List<float>();
            for (int i = 0; i < 64; i++) catTable.Add(0.0f);
            foreach (AutoResolveCombatant heroCandidate in units.Where(x => x.IsAlive && x.HasSpecialAbility))
            {
                foreach (KeyValuePair<string, float> kv in heroCandidate.SpecialAbilityUnitStrengthFactors)
                {
                    ulong heroMask = Parse_Category_Mask(kv.Key);
                    if (heroMask == 0UL) continue;

                    float weight = kv.Value - 1.0f;
                    int idx = Get_First_Bit_Set(heroMask);
                    if (idx > -1 && weight > catTable[idx])
                    {
                        catTable[idx] = weight;
                    }
                }
            }

            foreach (AutoResolveCombatant unit in units.Where(x => x.IsAlive))
            {
                int unitIndex = units.IndexOf(unit);
                int bestCategoryIndex = -1;

                if (!mIsSpace && unit.IsPlanet)
                {
                    if (unit.IncludePlanetTacticalBuiltObjects)
                    {
                        foreach (AutoResolveBuiltObject built in unit.PlanetBuiltObjects)
                        {
                            if (built.Power <= 0.0f) continue;

                            float remainingPower = built.Power;
                            while (remainingPower > 0.0f && bestCategoryIndex != 0)
                            {
                                Find_Contrast_Index(remainingPower, built.CategoryMask, result, out bestCategoryIndex);

                                AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                                engagement.AttackerOwnerId = playerId;
                                engagement.SourceTypeName = unit.TypeName;
                                engagement.SourceUnitIndex = unitIndex;
                                engagement.SourceKind = "BuiltObject";
                                engagement.SourcePowerBefore = remainingPower;
                                engagement.TargetCategory = bestCategoryIndex > 0 ? Get_Display_Category_Name(result[bestCategoryIndex].Category) : "(global)";
                                int targetIndex = bestCategoryIndex > 0 ? bestCategoryIndex : (mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX);
                                int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                                engagement.TargetCategoryBefore = result[targetIndex].Force;
                                engagement.TargetGlobalBefore = result[globalIndex].Force;

                                Apply_Unit_Contrast(ref remainingPower, built.CategoryMask, ref result, bestCategoryIndex, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                                engagement.TargetCategoryAfter = result[targetIndex].Force;
                                engagement.TargetGlobalAfter = result[globalIndex].Force;
                                mLastEngagements.Add(engagement);

                                bestCategoryIndex = 0;
                            }
                        }
                    }
                    continue;
                }

                bool addGarrison = unit.AddGarrison;
                if (addGarrison && unit.IsDummyStarBase && MidTactical)
                {
                    addGarrison = false;
                }

                if (addGarrison && unit.GarrisonEntries != null && unit.GarrisonEntries.Count > 0)
                {
                    for (int g = 0; g < unit.GarrisonEntries.Count; g++)
                    {
                        AutoResolveBuiltObject garrisonEntry = unit.GarrisonEntries[g];
                        if (garrisonEntry.Power <= 0.0f || garrisonEntry.CategoryMask == 0UL) continue;

                        float remainingPower = garrisonEntry.Power;
                        while (remainingPower > 0.0f && bestCategoryIndex != 0)
                        {
                            Find_Contrast_Index(remainingPower, garrisonEntry.CategoryMask, result, out bestCategoryIndex);

                            AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                            engagement.AttackerOwnerId = playerId;
                            engagement.SourceTypeName = unit.TypeName;
                            engagement.SourceUnitIndex = unitIndex;
                            engagement.SourceKind = "Garrison";
                            engagement.SourcePowerBefore = remainingPower;
                            engagement.SourceCategory = Get_Display_Category_Name(garrisonEntry.CategoryMask);
                            engagement.TargetCategory = bestCategoryIndex > 0 ? Get_Display_Category_Name(result[bestCategoryIndex].Category) : "(global)";
                            int targetIndex = bestCategoryIndex > 0 ? bestCategoryIndex : (mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX);
                            int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                            engagement.TargetCategoryBefore = result[targetIndex].Force;
                            engagement.TargetGlobalBefore = result[globalIndex].Force;

                            Apply_Unit_Contrast(ref remainingPower, garrisonEntry.CategoryMask, ref result, bestCategoryIndex, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                            engagement.TargetCategoryAfter = result[targetIndex].Force;
                            engagement.TargetGlobalAfter = result[globalIndex].Force;
                            mLastEngagements.Add(engagement);

                            bestCategoryIndex = 0;
                        }
                    }
                }

                if (unit.IsTransport && mIsSpace) continue;

                float unitRemainingPower = unit.Power;
                while (unitRemainingPower > 0.0f && bestCategoryIndex != 0)
                {
                    Find_Contrast_Index(unitRemainingPower, unit.CategoryMask, result, out bestCategoryIndex);

                    AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                    engagement.AttackerOwnerId = playerId;
                    engagement.SourceTypeName = unit.TypeName;
                    engagement.SourceUnitIndex = unitIndex;
                    engagement.SourceKind = "Unit";
                    engagement.SourcePowerBefore = unitRemainingPower;
                    engagement.TargetCategory = bestCategoryIndex > 0 ? Get_Display_Category_Name(result[bestCategoryIndex].Category) : "(global)";
                    int targetIndex = bestCategoryIndex > 0 ? bestCategoryIndex : (mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX);
                    int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                    engagement.TargetCategoryBefore = result[targetIndex].Force;
                    engagement.TargetGlobalBefore = result[globalIndex].Force;

                    Apply_Unit_Contrast(ref unitRemainingPower, unit.CategoryMask, ref result, bestCategoryIndex, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                    engagement.TargetCategoryAfter = result[targetIndex].Force;
                    engagement.TargetGlobalAfter = result[globalIndex].Force;
                    mLastEngagements.Add(engagement);

                    bestCategoryIndex = 0;
                }
            }
        }

        public void Calculate_Side_Force(List<AutoResolveCombatant> units, out TargetResult result, int playerId, out AutoResolveCombatant weakestUnit)
        {
            result = Create_Empty_Target_Result();
            weakestUnit = null;

            float weakestVal = float.MaxValue;

            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                if (!unit.IsAlive) continue;

                if (!mIsSpace && unit.IsPlanet)
                {
                    if (unit.IncludePlanetTacticalBuiltObjects)
                    {
                        foreach (AutoResolveBuiltObject built in unit.PlanetBuiltObjects)
                        {
                            if (built.Power <= 0f || built.CategoryMask == 0UL) continue;
                            int builtIndex = mContrastCategoryToIndex[built.CategoryMask];
                            result[builtIndex].Force += built.Power;
                            result[builtIndex].Ground = true;

                            int groundIndex = GLOBAL_GROUND_INDEX;
                            result[groundIndex].Force += built.Power;
                            result[groundIndex].Ground = true;
                        }
                    }
                    continue;
                }

                bool addGarrison = unit.AddGarrison;
                if (addGarrison && unit.IsDummyStarBase && MidTactical) addGarrison = false;
                if (addGarrison && unit.GarrisonEntries != null)
                {
                    int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;

                    for (int g = 0; g < unit.GarrisonEntries.Count; g++)
                    {
                        AutoResolveBuiltObject garrisonEntry = unit.GarrisonEntries[g];
                        if (garrisonEntry.Power <= 0f || garrisonEntry.CategoryMask == 0UL) continue;

                        int garrisonIndex = mContrastCategoryToIndex[garrisonEntry.CategoryMask];
                        result[garrisonIndex].Force += garrisonEntry.Power;
                        result[garrisonIndex].Ground = !mIsSpace;

                        result[globalIndex].Force += garrisonEntry.Power;
                        result[globalIndex].Ground = !mIsSpace;
                    }
                }

                if (unit.IsTransport && mIsSpace) continue;

                if (unit.Power < weakestVal)
                {
                    weakestUnit = unit;
                    weakestVal = unit.Power;
                }

                ulong ctype = unit.CategoryMask;
                int cval = Get_First_Bit_Set(ctype);
                if (cval > -1)
                {
                    ulong category = 1UL << cval;
                    int unitCategoryIndex = mContrastCategoryToIndex[category];
                    result[unitCategoryIndex].Force += unit.Power;
                    result[unitCategoryIndex].Ground = !mIsSpace;
                }

                int unitGlobalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                result[unitGlobalIndex].Force += unit.Power;
                result[unitGlobalIndex].Ground = !mIsSpace;
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
                mLastWinnerDecision = "Super weapon rule: attacker has super weapon and defender lacks killer; defender is forced to retreat.";
            }
            else if (mSides[1].SuperWeaponPresent && !mSides[0].SuperWeaponKillerPresent)
            {
                Player_Retreats(mSides[0].OwnerId);
                mLastWinnerDecision = "Super weapon rule: defender has super weapon and attacker lacks killer; attacker is forced to retreat.";
            }

            if (mRetreatInProgress)
            {
                int retreatWinner = mRetreatingPlayer == mSides[0].OwnerId ? 1 : 0;
                mLastWinnerDecision = mLastWinnerDecision + " Winner selected by retreat state: retreating owner=" + mRetreatingPlayer.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
                return retreatWinner;
            }

            float totalA = 0.0f;
            bool anyPositiveA = false;
            for (int i = 0; i < resultsA.Count; i++)
            {
                float force = resultsA[i].Force;
                if (force > 0.0f)
                {
                    anyPositiveA = true;
                    totalA += force;
                }
            }

            float totalB = 0.0f;
            bool anyPositiveB = false;
            for (int i = 0; i < resultsB.Count; i++)
            {
                float force = resultsB[i].Force;
                if (force > 0.0f)
                {
                    anyPositiveB = true;
                    totalB += force;
                }
            }

            if (anyPositiveA && anyPositiveB)
            {
                // Placeholder: human-vs-ai and playable-faction tie-breaks require full PlayerClass/Faction data.
                int winner = totalA > totalB ? 0 : 1;
                mLastWinnerDecision = "Both sides have positive force; compare totals A=" + totalA.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " vs B=" + totalB.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ".";
                return winner;
            }
            else if ((anyPositiveA || anyPositiveB) && Math.Abs(totalA - totalB) > 0.0001f)
            {
                int winner = totalA > totalB ? 0 : 1;
                mLastWinnerDecision = "Only one side has effective remaining force; compare totals A=" + totalA.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " vs B=" + totalB.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ".";
                return winner;
            }
            else
            {
                int winner = mAggressor == mSides[0].OwnerId ? 0 : 1;
                mLastWinnerDecision = "Totals are tied/zero; winner defaults to aggressor owner=" + mAggressor.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
                return winner;
            }
        }

        private bool SideHasSuperWeaponKiller(int side)
        {
            if (side < 0 || side >= mSides.Length) return false;
            return mSides[side].Queue.Any(x => x != null && x.IsAlive && x.IsSuperWeaponKiller);
        }

        private void Build_Contrast_Index_Map()
        {
            mContrastCategoryToIndex.Clear();
            mContrastIndexToCategory.Clear();
            mContrastCategoryDisplayName.Clear();

            Add_Contrast_Index(GLOBAL_GROUND_MASK, "(global)", true);
            Add_Contrast_Index(GLOBAL_SPACE_MASK, "(global space)", false);

            Add_Contrast_Categories_From_Side(mSides[0]);
            Add_Contrast_Categories_From_Side(mSides[1]);
        }

        private void Add_Contrast_Categories_From_Side(SideStruct side)
        {
            for (int i = 0; i < side.Queue.Count; i++)
            {
                AutoResolveCombatant combatant = side.Queue[i];

                ulong ctype = combatant.CategoryMask;
                for (int bit = 0; bit < 64; bit++)
                {
                    ulong categoryMask = 1UL << bit;
                    if ((ctype & categoryMask) != 0UL)
                    {
                        Add_Contrast_Index(categoryMask, Get_Display_Category_Name(categoryMask), !mIsSpace);
                    }
                }

                if (combatant.PlanetBuiltObjects != null)
                {
                    for (int j = 0; j < combatant.PlanetBuiltObjects.Count; j++)
                    {
                        AutoResolveBuiltObject built = combatant.PlanetBuiltObjects[j];
                        if (built.CategoryMask != 0UL)
                        {
                            Add_Contrast_Index(built.CategoryMask, Get_Display_Category_Name(built.CategoryMask), true);
                        }
                    }
                }

                if (combatant.GarrisonEntries != null)
                {
                    for (int j = 0; j < combatant.GarrisonEntries.Count; j++)
                    {
                        AutoResolveBuiltObject garrison = combatant.GarrisonEntries[j];
                        if (garrison.CategoryMask != 0UL)
                        {
                            Add_Contrast_Index(garrison.CategoryMask, Get_Display_Category_Name(garrison.CategoryMask), !mIsSpace);
                        }
                    }
                }
            }
        }

        private void Add_Contrast_Index(ulong categoryMask, string displayName, bool ground)
        {
            int index = mContrastIndexToCategory.Count;
            if (index == GLOBAL_GROUND_INDEX || index == GLOBAL_SPACE_INDEX)
            {
                mContrastIndexToCategory.Add(categoryMask);
                return;
            }

            if (mContrastCategoryToIndex.ContainsKey(categoryMask)) return;

            mContrastCategoryToIndex[categoryMask] = index;
            mContrastIndexToCategory.Add(categoryMask);
            mContrastCategoryDisplayName[categoryMask] = displayName;
        }

        private TargetResult Create_Empty_Target_Result()
        {
            TargetResult result = new TargetResult();
            result.Entries = new List<TargetResult.ContrastForceStruct>(mContrastIndexToCategory.Count);

            for (int i = 0; i < mContrastIndexToCategory.Count; i++)
            {
                ulong categoryMask = mContrastIndexToCategory[i];
                bool ground = i == GLOBAL_GROUND_INDEX;
                if (i > GLOBAL_SPACE_INDEX) ground = !mIsSpace;
                result.Entries.Add(new TargetResult.ContrastForceStruct(categoryMask, 0f, ground));
            }

            return result;
        }

        private int Get_First_Bit_Set(ulong value)
        {
            for (int bit = 0; bit < 64; bit++)
            {
                if ((value & (1UL << bit)) != 0UL) return bit;
            }

            return -1;
        }

        private string Get_Display_Category_Name(ulong categoryMask)
        {
            if (categoryMask == 0UL) return "(global)";

            if (CategoryNameProvider != null)
            {
                string external = CategoryNameProvider(categoryMask);
                if (!string.IsNullOrWhiteSpace(external)) return external;
            }

            string existing;
            if (mContrastCategoryDisplayName.TryGetValue(categoryMask, out existing)) return existing;

            for (int bit = 0; bit < 64; bit++)
            {
                ulong single = 1UL << bit;
                if ((categoryMask & single) == 0UL) continue;

                string singleName;
                if (mContrastCategoryDisplayName.TryGetValue(single, out singleName)) return singleName;
            }

            return "0x" + categoryMask.ToString("X16");
        }

        private ulong Parse_Category_Mask(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return 0UL;
            if (category.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToUInt64(category.Substring(2), 16);

            if (CategoryMaskProvider != null)
            {
                ulong external = CategoryMaskProvider(category);
                if (external != 0UL) return external;
            }

            foreach (KeyValuePair<ulong, string> kv in mContrastCategoryDisplayName)
            {
                if (string.Equals(kv.Value, category, StringComparison.OrdinalIgnoreCase)) return kv.Key;
            }

            return 0UL;
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
            mLastEngagements.Clear();
            mBattleFought = false;

            mBattleID = (mBattleID + 1) % MAX_HISTORY;
            mBattleHistory[mBattleID] = new AutoResolveBattle();
        }
    }
}
