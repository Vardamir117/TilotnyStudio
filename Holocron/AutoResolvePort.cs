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
        public List<AutoResolveBuiltObject> GarrisonEntries = new List<AutoResolveBuiltObject>();
        public float PlanetaryReplacementPower = -1f;
        public bool IncludePlanetTacticalBuiltObjects;
        public List<AutoResolveBuiltObject> PlanetBuiltObjects = new List<AutoResolveBuiltObject>();
        public bool AddGarrison = true;
        public bool IsDummyStarBase;

        // Placeholder for C++ hero special ability strength factors (category -> multiplier).
        public bool HasSpecialAbility;
        public Dictionary<string, float> SpecialAbilityUnitStrengthFactors = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public List<string> ContrastCategories = new List<string>();
    }

    public class AutoResolveBuiltObject
    {
        public string ContrastCategory;
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

        public bool MidTactical { get; set; }
        public float TacticalMultiplier { get; set; } = 1.0f;

        public float LoserAttrition { get; set; } = 0.35f;
        public float WinnerAttrition { get; set; } = 0.15f;
        public float RetreatLoserAttrition { get; set; } = 0.35f;
        public float RetreatWinnerAttrition { get; set; } = 0.15f;
        public float AttritionAllowanceFactor { get; set; } = 0.333333f;
        public float TransportLosses { get; set; } = 0.333333f;

        // Optional provider for PGAICommands contrast weighting: (enemyCategory, friendlyCategory) => weight.
        public Func<string, string, float> ContrastWeightProvider { get; set; }

        // The original implementation supports two participating sides.
        private readonly SideStruct[] mSides = new SideStruct[] { new SideStruct(), new SideStruct() };

        // Circular battle history buffer (MAX_HISTORY entries).
        private readonly AutoResolveBattle[] mBattleHistory = new AutoResolveBattle[MAX_HISTORY];

        // C++-style deterministic contrast index layout for ResultType access by index.
        private readonly Dictionary<string, int> mContrastCategoryToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> mContrastIndexToCategory = new List<string>();

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
            int spaceIndex = mContrastCategoryToIndex["__GLOBAL_SPACE__"];
            float side0SpaceForce = mSides[0].TotalForce[spaceIndex].Force;
            float side1SpaceForce = mSides[1].TotalForce[spaceIndex].Force;

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

            // C++ parity: Side_Attack performs explicit result = target_force cloning.
            TargetResult[] results = new TargetResult[2];

            mLastEngagements.Clear();
            mLastAttritionReports.Clear();
            Side_Attack(mSides[0].Queue, mSides[1].TotalForce, ref results[1], mSides[0].OwnerId);
            Side_Attack(mSides[1].Queue, mSides[0].TotalForce, ref results[0], mSides[1].OwnerId);

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

                int globalIndex = mContrastCategoryToIndex[globalKey];
                float loserTotal = mSides[loser].TotalForce[globalIndex].Force;
                results[loser][globalIndex].Force = results[loser][globalIndex].Force + ((loserTotal - results[loser][globalIndex].Force) * (1.0f - loserAttritionValue));

                float winnerTotal = mSides[winner].TotalForce[globalIndex].Force;
                results[winner][globalIndex].Force = results[winner][globalIndex].Force + ((winnerTotal - results[winner][globalIndex].Force) * (1.0f - winnerAttritionValue));

                mSides[loser].WeakestUnit = null;

                if (piratePlayer)
                {
                    results[loser][globalIndex].Force = 0.0f;
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
            mLastEngagements.Clear();
            mLastAttritionReports.Clear();
            mBattleFought = false;
            mSides[0].Init();
            mSides[1].Init();
            return AutoResolveHResult.S_OK;
        }

        public int Who_Won() { return mWinningPlayer; }
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

            int global_index = mIsSpace ? 1 : 0;
            int sideOwnerId = (index >= 0 && index < mSides.Length) ? mSides[index].OwnerId : -1;

            // MLL: Hack to make tactical auto resolve less efficient.
            if (MidTactical)
            {
                float tacticalFactor = TacticalMultiplier;
                for (int i = 0; i < current.Count; i++)
                {
                    current[i].Force *= tacticalFactor;
                }
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
                        if (unit.IncludeGarrisonInAttrition && unit.GarrisonPower > 0f)
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
                        else
                        {
                            if (current[global_index].Force - (unit.Power * attritionAllowanceFactor) > 0.0f)
                            {
                                current[global_index].Force -= unit.Power;
                                current[global_index].Force = Math.Max(current[global_index].Force, 0.0f);
                                killUnit = false;

                                // logging info
                                attritionReport.Decision = "KeepUnit";
                                attritionReport.Notes = "Unit survives by attrition allowance check.";
                            }
                            else
                            {
                                // logging info
                                attritionReport.Decision = "KillUnit";
                                if (string.IsNullOrEmpty(attritionReport.Notes))
                                {
                                    attritionReport.Notes = "Insufficient remaining force after attrition allowance check.";
                                }
                            }
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
            for (int i = 0; i < current.Count; i++)
            {
                string enemyCategory = current[i].Category;
                if (string.IsNullOrWhiteSpace(enemyCategory) || enemyCategory.StartsWith("__GLOBAL_", StringComparison.OrdinalIgnoreCase)) continue;

                float remaining = current[i].Force;
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

        public void Apply_Unit_Contrast(ref float remainingPower, AutoResolveCombatant unit, ref TargetResult current, string bestCategory, Dictionary<string, float> factorTable, MapEnvironmentType terrain, AutoResolveEngagementReport engagement)
        {
            if (unit == null) return;

            float originalPower = remainingPower;
            float factor = 0.0f;
            // logging info
            float heroMultiplier = 1.0f;
            float contrastMultiplier = 1.0f;

            if (factorTable != null && unit.ContrastCategories != null)
            {
                for (int i = 0; i < unit.ContrastCategories.Count; i++)
                {
                    string category = unit.ContrastCategories[i];
                    float categoryFactor;
                    if (!string.IsNullOrWhiteSpace(category) && factorTable.TryGetValue(category, out categoryFactor) && categoryFactor > factor)
                    {
                        factor = categoryFactor;
                    }
                }
            }

            if (factor != 0.0f)
            {
                remainingPower *= factor;
                // logging info
                heroMultiplier = factor;
            }

            string globalKey = mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__";

            if (!string.IsNullOrWhiteSpace(bestCategory))
            {
                float contrastWeight = Get_Unit_Contrast_Weight(unit, bestCategory);
                remainingPower *= contrastWeight;
                // logging info
                contrastMultiplier = contrastWeight;
                engagement.ScaledPower = remainingPower;
                if (string.IsNullOrWhiteSpace(engagement.SourceCategory))
                {
                    engagement.SourceCategory = Get_Best_Source_Category_For_Target(unit, bestCategory);
                }

                int targetIndex = mContrastCategoryToIndex[bestCategory];

                float modifiedForceApplied = Math.Min(current[targetIndex].Force, remainingPower);
                remainingPower = (1.0f - modifiedForceApplied / remainingPower) * originalPower;

                current[targetIndex].Force -= modifiedForceApplied;

                int globalIndex = mContrastCategoryToIndex[globalKey];
                current[globalIndex].Force -= (modifiedForceApplied + remainingPower);

                // logging info
                engagement.AppliedCombatPower = modifiedForceApplied;
            }
            else
            {
                int globalIndex = mContrastCategoryToIndex[globalKey];
                float appliedToGlobal = remainingPower;
                engagement.ScaledPower = appliedToGlobal;
                engagement.SourceCategory = unit.ContrastCategories == null ? null : unit.ContrastCategories.FirstOrDefault();
                current[globalIndex].Force -= appliedToGlobal;
                remainingPower = 0.0f;

                // logging info
                engagement.AppliedCombatPower = appliedToGlobal;
            }

            // logging info
            engagement.HeroMultiplier = heroMultiplier;
            engagement.ContrastMultiplier = contrastMultiplier;
            engagement.TotalMultiplier = heroMultiplier * contrastMultiplier;
            engagement.SourcePowerAfter = remainingPower;
        }
           
        // logging util
        private string Get_Best_Source_Category_For_Target(AutoResolveCombatant unit, string targetCategory)
        {
            if (unit == null || unit.ContrastCategories == null || unit.ContrastCategories.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(targetCategory)) return unit.ContrastCategories[0];

            string bestCategory = null;
            float bestWeight = float.MinValue;

            for (int i = 0; i < unit.ContrastCategories.Count; i++)
            {
                string friendlyCategory = unit.ContrastCategories[i];
                if (string.IsNullOrWhiteSpace(friendlyCategory)) continue;

                float weight = ContrastWeightProvider == null ? 1.0f : ContrastWeightProvider(targetCategory, friendlyCategory);

                // Match Get_Average_Contrast_Factor semantics: ignore default/no-op 1.0 mappings.
                if (Math.Abs(weight - 1.0f) <= 0.0001f) continue;

                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    bestCategory = friendlyCategory;
                }
            }

            return bestCategory ?? unit.ContrastCategories[0];
        }

        public void Side_Attack(List<AutoResolveCombatant> units, TargetResult targetForce, ref TargetResult result, int playerId)
        {
            // C++ parity: start from target_force snapshot.
            result = targetForce.Clone();

            // C++ cat_table equivalent: best hero strength bonus per category (stored as multiplier component).
            Dictionary<string, float> catTable = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (AutoResolveCombatant heroCandidate in units.Where(x => x != null && x.IsAlive && x.HasSpecialAbility))
            {
                foreach (KeyValuePair<string, float> kv in heroCandidate.SpecialAbilityUnitStrengthFactors)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    float weight = kv.Value - 1.0f;
                    float extant;
                    if (!catTable.TryGetValue(kv.Key, out extant) || weight > extant)
                    {
                        catTable[kv.Key] = weight;
                    }
                }
            }

            // Placeholder: mBombingRun/mBomberType behavior requires explicit bomber source in the C# model.

            foreach (AutoResolveCombatant unit in units.Where(x => x != null && x.IsAlive))
            {
                int unitIndex = units.IndexOf(unit);
                string bestCategory = "__UNSET__";

                if (!mIsSpace && unit.IsPlanet)
                {
                    if (unit.IncludePlanetTacticalBuiltObjects)
                    {
                        foreach (AutoResolveBuiltObject built in unit.PlanetBuiltObjects)
                        {
                            if (built == null || built.Power <= 0.0f) continue;

                            float remainingPower = built.Power;
                            while (remainingPower > 0.0f && bestCategory != null)
                            {
                                Find_Contrast_Index(remainingPower, unit, result, out bestCategory);

                                AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                                engagement.AttackerOwnerId = playerId;
                                engagement.SourceTypeName = unit.TypeName;
                                engagement.SourceUnitIndex = unitIndex;
                                engagement.SourceKind = "BuiltObject";
                                engagement.SourcePowerBefore = remainingPower;
                                engagement.TargetCategory = string.IsNullOrWhiteSpace(bestCategory) ? "(global)" : bestCategory;
                                int targetIndex = string.IsNullOrWhiteSpace(bestCategory) ? 0 : mContrastCategoryToIndex[bestCategory];
                                int globalIndex = mContrastCategoryToIndex[mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__"];
                                engagement.TargetCategoryBefore = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                                engagement.TargetGlobalBefore = result[globalIndex].Force;

                                Apply_Unit_Contrast(ref remainingPower, unit, ref result, bestCategory, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                                engagement.TargetCategoryAfter = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                                engagement.TargetGlobalAfter = result[globalIndex].Force;
                                mLastEngagements.Add(engagement);

                                bestCategory = null;
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
                        if (garrisonEntry == null || garrisonEntry.Power <= 0.0f || string.IsNullOrWhiteSpace(garrisonEntry.ContrastCategory)) continue;

                        AutoResolveCombatant garrisonCombatant = new AutoResolveCombatant();
                        garrisonCombatant.TypeName = unit.TypeName;
                        garrisonCombatant.OwnerId = unit.OwnerId;
                        garrisonCombatant.ContrastCategories = new List<string> { garrisonEntry.ContrastCategory };

                        float remainingPower = garrisonEntry.Power;
                        while (remainingPower > 0.0f && bestCategory != null)
                        {
                            Find_Contrast_Index(remainingPower, garrisonCombatant, result, out bestCategory);

                            AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                            engagement.AttackerOwnerId = playerId;
                            engagement.SourceTypeName = unit.TypeName;
                            engagement.SourceUnitIndex = unitIndex;
                            engagement.SourceKind = "Garrison";
                            engagement.SourcePowerBefore = remainingPower;
                            engagement.SourceCategory = garrisonEntry.ContrastCategory;
                            engagement.TargetCategory = string.IsNullOrWhiteSpace(bestCategory) ? "(global)" : bestCategory;
                            int targetIndex = string.IsNullOrWhiteSpace(bestCategory) ? 0 : mContrastCategoryToIndex[bestCategory];
                            int globalIndex = mContrastCategoryToIndex[mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__"];
                            engagement.TargetCategoryBefore = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                            engagement.TargetGlobalBefore = result[globalIndex].Force;

                            Apply_Unit_Contrast(ref remainingPower, garrisonCombatant, ref result, bestCategory, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                            engagement.TargetCategoryAfter = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                            engagement.TargetGlobalAfter = result[globalIndex].Force;
                            mLastEngagements.Add(engagement);

                            bestCategory = null;
                        }
                    }
                }

                if (unit.IsTransport && mIsSpace) continue;

                float unitRemainingPower = unit.Power;
                while (unitRemainingPower > 0.0f && bestCategory != null)
                {
                    Find_Contrast_Index(unitRemainingPower, unit, result, out bestCategory);

                    //logging info
                    AutoResolveEngagementReport engagement = new AutoResolveEngagementReport();
                    engagement.AttackerOwnerId = playerId;
                    engagement.SourceTypeName = unit.TypeName;
                    engagement.SourceUnitIndex = unitIndex;
                    engagement.SourceKind = "Unit";
                    engagement.SourcePowerBefore = unitRemainingPower;
                    engagement.TargetCategory = string.IsNullOrWhiteSpace(bestCategory) ? "(global)" : bestCategory;
                    int targetIndex = string.IsNullOrWhiteSpace(bestCategory) ? 0 : mContrastCategoryToIndex[bestCategory];
                    int globalIndex = mContrastCategoryToIndex[mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__"];
                    engagement.TargetCategoryBefore = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                    engagement.TargetGlobalBefore = result[globalIndex].Force;

                    Apply_Unit_Contrast(ref unitRemainingPower, unit, ref result, bestCategory, catTable, mIsSpace ? MapEnvironmentType.Space : MapEnvironmentType.Ground, engagement);

                    //logging info
                    engagement.TargetCategoryAfter = string.IsNullOrWhiteSpace(bestCategory) ? 0.0f : result[targetIndex].Force;
                    engagement.TargetGlobalAfter = result[globalIndex].Force;
                    mLastEngagements.Add(engagement);

                    bestCategory = null;
                }
            }
        }

        public void Calculate_Side_Force(List<AutoResolveCombatant> units, out TargetResult result, int playerId, out AutoResolveCombatant weakestUnit)
        {
            // C++ parity: result has global ground/space buckets plus contrast-type buckets.
            // Placeholder: TargetContrastClass::Init_Contrast_Type_List and typed result slots are represented by category keys.
            result = Create_Empty_Target_Result();
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
                            int builtIndex = mContrastCategoryToIndex[built.ContrastCategory];
                            result[builtIndex].Force += built.Power;
                            result[builtIndex].Ground = true;

                            int groundIndex = mContrastCategoryToIndex["__GLOBAL_GROUND__"];
                            result[groundIndex].Force += built.Power;
                            result[groundIndex].Ground = true;
                        }
                    }
                    continue;
                }

                // C++: Apply garrison units unless in suppressed dummy-starbase mid-tactical path.
                bool addGarrison = unit.AddGarrison;
                if (addGarrison && unit.IsDummyStarBase && MidTactical) addGarrison = false;
                if (addGarrison && unit.GarrisonEntries != null)
                {
                    string globalKey = mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__";
                    int globalIndex = mContrastCategoryToIndex[globalKey];

                    for (int g = 0; g < unit.GarrisonEntries.Count; g++)
                    {
                        AutoResolveBuiltObject garrisonEntry = unit.GarrisonEntries[g];
                        if (garrisonEntry == null || garrisonEntry.Power <= 0f || string.IsNullOrWhiteSpace(garrisonEntry.ContrastCategory)) continue;
                        if (!mContrastCategoryToIndex.ContainsKey(garrisonEntry.ContrastCategory)) continue;

                        float totalForce = garrisonEntry.Power;
                        int garrisonIndex = mContrastCategoryToIndex[garrisonEntry.ContrastCategory];
                        result[garrisonIndex].Force += totalForce;
                        result[garrisonIndex].Ground = !mIsSpace;

                        result[globalIndex].Force += totalForce;
                        result[globalIndex].Ground = !mIsSpace;
                    }
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
                if (!string.IsNullOrWhiteSpace(unitCategory))
                {
                    int unitCategoryIndex = mContrastCategoryToIndex[unitCategory];
                    result[unitCategoryIndex].Force += unit.Power;
                    result[unitCategoryIndex].Ground = false;
                }

                string unitGlobalKey = mIsSpace ? "__GLOBAL_SPACE__" : "__GLOBAL_GROUND__";
                int unitGlobalIndex = mContrastCategoryToIndex[unitGlobalKey];
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

        private bool SideHasSuperWeaponKiller(int side)
        {
            if (side < 0 || side >= mSides.Length) return false;
            return mSides[side].Queue.Any(x => x != null && x.IsAlive && x.IsSuperWeaponKiller);
        }

        private float Get_Unit_Contrast_Weight(AutoResolveCombatant unit, string enemyCategory)
        {
            if (unit == null) return 0.0f;
            if (unit.ContrastCategories == null || unit.ContrastCategories.Count == 0) return 0.0f;
            if (string.IsNullOrWhiteSpace(enemyCategory)) return 0.0f;

            return TargetContrastPort.Get_Average_Contrast_Factor(
                unit.ContrastCategories,
                enemyCategory,
                ContrastWeightProvider);
        }

        private void Build_Contrast_Index_Map()
        {
            mContrastCategoryToIndex.Clear();
            mContrastIndexToCategory.Clear();

            Add_Contrast_Index("__GLOBAL_GROUND__", true);
            Add_Contrast_Index("__GLOBAL_SPACE__", false);

            Add_Contrast_Categories_From_Side(mSides[0]);
            Add_Contrast_Categories_From_Side(mSides[1]);
        }

        private void Add_Contrast_Categories_From_Side(SideStruct side)
        {
            if (side == null || side.Queue == null) return;

            for (int i = 0; i < side.Queue.Count; i++)
            {
                AutoResolveCombatant combatant = side.Queue[i];
                if (combatant == null) continue;

                if (combatant.ContrastCategories != null)
                {
                    for (int j = 0; j < combatant.ContrastCategories.Count; j++)
                    {
                        string category = combatant.ContrastCategories[j];
                        if (string.IsNullOrWhiteSpace(category)) continue;
                        Add_Contrast_Index(category, false);
                    }
                }

                if (combatant.PlanetBuiltObjects != null)
                {
                    for (int j = 0; j < combatant.PlanetBuiltObjects.Count; j++)
                    {
                        AutoResolveBuiltObject built = combatant.PlanetBuiltObjects[j];
                        if (built == null || string.IsNullOrWhiteSpace(built.ContrastCategory)) continue;
                        Add_Contrast_Index(built.ContrastCategory, true);
                    }
                }

                if (combatant.GarrisonEntries != null)
                {
                    for (int j = 0; j < combatant.GarrisonEntries.Count; j++)
                    {
                        AutoResolveBuiltObject garrison = combatant.GarrisonEntries[j];
                        if (garrison == null || string.IsNullOrWhiteSpace(garrison.ContrastCategory)) continue;
                        Add_Contrast_Index(garrison.ContrastCategory, !mIsSpace);
                    }
                }
            }
        }

        private void Add_Contrast_Index(string category, bool ground)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            if (mContrastCategoryToIndex.ContainsKey(category)) return;

            int index = mContrastIndexToCategory.Count;
            mContrastCategoryToIndex[category] = index;
            mContrastIndexToCategory.Add(category);
        }

        private TargetResult Create_Empty_Target_Result()
        {
            TargetResult result = new TargetResult();
            result.Entries = new List<TargetResult.ContrastForceStruct>(mContrastIndexToCategory.Count);

            for (int i = 0; i < mContrastIndexToCategory.Count; i++)
            {
                string category = mContrastIndexToCategory[i];
                bool ground = category.IndexOf("GROUND", StringComparison.OrdinalIgnoreCase) >= 0;
                result.Entries.Add(new TargetResult.ContrastForceStruct(category, 0f, ground));
            }

            return result;
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
