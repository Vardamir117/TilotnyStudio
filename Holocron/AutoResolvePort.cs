using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// C# port of AutoResolveClass (FOC/Code/RTS/AutoResolve.cpp) for galactic auto-resolve.
//
// Reference material, in order of authority:
//   1. StarWarsI64/StarWarsG x64 build (MSVC 14.29) decompilation:
//      decomp/decompilations_by_source/generated_x64/FOC/Code/RTS/AutoResolve.h.decomp.cpp
//   2. Original source: decomp/autoresolve source/AutoResolve.cpp, TargetContrast.cpp
// Where the shipped x64 binary differs from the source, the binary is followed; each such place is
// marked "x64:" below. Holocron/AutoResolveValidation.md lists every known difference and gap.

namespace Holocron
{
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
        E_AUTORESOLVE_RETREATING = 9,
        E_AUTORESOLVE_ADDING_TWICE = 10,
        E_AUTORESOLVE_TOO_MANY_SIDES = 11
    }

    // Mirrors AutoResolveBattle from AutoResolve.h: battle location and kill log.
    public class AutoResolveBattle
    {
        public string Planet;
        public List<KeyValuePair<string, int>> Killed = new List<KeyValuePair<string, int>>();
    }

    // The subset of GameObjectTypeClass that auto-resolve reads.
    public class AutoResolveObjectType
    {
        public string Name;
        public float AICombatPower;                 // Get_AI_Combat_Power_Metric() in galactic mode
        public ulong CategoryMask;
        public bool IsTransport;                    // Behaves_Like(BEHAVIOR_TRANSPORT)
        public bool IsDummyGroundBase;              // Behaves_Like(BEHAVIOR_DUMMY_GROUND_BASE)
        public bool IsDummyStarBase;                // Behaves_Like(BEHAVIOR_DUMMY_STAR_BASE)
        public bool IsDummyGroundStructure;         // object has BEHAVIOR_DUMMY_GROUND_STRUCTURE
        public bool IsNamedHero;
        public bool IsSuperWeaponKiller;
        public string MovementClass;                // only used for terrain effectiveness
        public List<AutoResolveObjectType> GroundCompanyUnits = new List<AutoResolveObjectType>();
        public List<AutoResolveObjectType> SquadronUnits = new List<AutoResolveObjectType>();
        public AutoResolveObjectType UniqueSpaceContainerUnit;
        public AutoResolveObjectType UniqueGroundContainerUnit;

        // Starting_Spawned_Units_Tech_0..5, in XML order. A null/empty level falls back to the next lower
        // non-empty level (Get_Spawned_Unit_Type_Count / _Type / _Starting_Count).
        public List<AutoResolveSpawnedUnit>[] StartingSpawnedUnits = new List<AutoResolveSpawnedUnit>[6];

        // Hero galactic special ability strength factors (Get_Special_Ability_Unit_Strength_Factors):
        // category mask -> multiplier. Not populated by Holocron yet.
        public bool HasSpecialAbility;
        public List<KeyValuePair<ulong, float>> SpecialAbilityUnitStrengthFactors = new List<KeyValuePair<ulong, float>>();

        public List<AutoResolveSpawnedUnit> Get_Spawned_Units(int techLevel)
        {
            if (techLevel < 0 || techLevel >= StartingSpawnedUnits.Length) return new List<AutoResolveSpawnedUnit>();
            for (int tech = techLevel; tech >= 0; tech--)
            {
                List<AutoResolveSpawnedUnit> list = StartingSpawnedUnits[tech];
                if (list != null && list.Count != 0) return list;
            }
            return new List<AutoResolveSpawnedUnit>();
        }

        // Get_Spawned_Unit_Starting_Count returns the count of the first entry of that type.
        public int Get_Spawned_Unit_Starting_Count(int techLevel, AutoResolveObjectType spawnedType)
        {
            foreach (AutoResolveSpawnedUnit entry in Get_Spawned_Units(techLevel))
            {
                if (ReferenceEquals(entry.Type, spawnedType)) return entry.StartingCount;
            }
            return 0;
        }

        public override string ToString() { return Name; }
    }

    public class AutoResolveSpawnedUnit
    {
        public AutoResolveObjectType Type;          // may be null when the XML names an unknown type
        public int StartingCount;
    }

    // One ICombatantBehaviorClass in a side's queue.
    public class AutoResolveCombatant
    {
        public AutoResolveObjectType Type;          // Get_Original_Object_Type()
        public AutoResolveObjectType CurrentType;   // Get_Type(); null means same as Type
        public int OwnerId;
        public bool IsAlive = true;

        // Galactic objects have no parent-mode ID, so their spawned units ("garrison") are counted.
        // Only objects brought in from a parent mode during mid-tactical auto-resolve have one.
        public bool HasParentModeId;

        // Transport contents (Get_Transport_Contents). Null means "use Type.GroundCompanyUnits".
        public List<AutoResolveObjectType> TransportContents;

        // Planet (BaseCombatantClass) support. Holocron's UI does not add planets yet.
        public bool IsPlanet;
        public AutoResolveObjectType PlanetBaseType;                        // combatant->Get_Object_Type()
        public List<AutoResolveObjectType> PlanetBuiltObjects = new List<AutoResolveObjectType>();
        public Func<double, float, AutoResolveObjectType> BaseTypeForCombatRating; // Get_Base_Type_For_Combat_Rating

        public string TypeName { get { return Type == null ? "(none)" : Type.Name; } }
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
        public double TargetCategoryBefore;
        public double TargetCategoryAfter;
        public double TargetGlobalBefore;
        public double TargetGlobalAfter;
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
        public double ForceBefore;
        public double ForceAfter;
        public string Decision;
        public string Notes;
    }

    // Port of PGLib RandomClass (SyncRandom). Internal_Get: Seed = Seed * 0x41c64e6d + 0xbdf;
    // returns (Seed >> 10) & 0x7fff. Get(min, max) rejection-samples with a power-of-two mask.
    public class AutoResolveRandom
    {
        public uint Seed;

        public AutoResolveRandom(uint seed) { Seed = seed; }

        private uint Internal_Get()
        {
            Seed = unchecked(Seed * 0x41c64e6dU + 0xbdfU);
            return (Seed >> 10) & 0x7fffU;
        }

        public int Get(int min, int max)
        {
            if (max < min) { int t = min; min = max; max = t; }
            if (min == max) return min;
            int range = max - min;
            if ((uint)range > 0x7ffe) range = 0x7ffe;
            uint mask = 0x4000;
            while ((range & mask) == 0 && mask != 0) mask >>= 1;
            uint value;
            do
            {
                value = Internal_Get() & (mask * 2 - 1);
            } while (range < (int)value);
            return (int)value + min;
        }
    }

    public class AutoResolveClass
    {
        private const int MAX_HISTORY = 8;
        private const int GLOBAL_GROUND_INDEX = 0;
        private const int GLOBAL_SPACE_INDEX = 1;
        private const float BIG_FLOAT = 1e18f;      // x64: weakest_val initial value

        private class SideStruct
        {
            public int OwnerId = -1;
            public int UnitCount;
            public List<AutoResolveCombatant> Queue = new List<AutoResolveCombatant>();
            public AutoResolveCombatant WeakestUnit;
            public AutoResolveCombatant SuperWeapon;
            public AutoResolveCombatant SuperWeaponKiller;
            public TargetResult TotalForce = TargetResult.Create(2);

            public void Init()
            {
                OwnerId = -1;
                UnitCount = 0;
                Queue.Clear();
                WeakestUnit = null;
                SuperWeapon = null;
                SuperWeaponKiller = null;
                TotalForce = TargetResult.Create(2);
            }

            // SideStruct::Add_Combatant: append only (x64 0x14005c0d1 confirms no sort).
            public AutoResolveHResult Add_Combatant(AutoResolveCombatant combatant)
            {
                if (Queue.Contains(combatant)) return AutoResolveHResult.E_AUTORESOLVE_ADDING_TWICE;
                Queue.Add(combatant);
                UnitCount++;
                return AutoResolveHResult.S_OK;
            }
        }

        private bool mIsCombatPrepared;
        private bool mIsCombatInitiated;
        private bool mRetreatInProgress;
        private int mAggressor = -1;
        private int mRetreatingPlayer = -1;
        private int mWinningPlayer = -1;
        private bool mIsSpace;
        private bool mBattleFought;
        private int mBattleID = -1;
        private readonly AutoResolveBattle[] mBattleHistory = new AutoResolveBattle[MAX_HISTORY];
        private readonly SideStruct[] mSides = new SideStruct[] { new SideStruct(), new SideStruct() };

        private readonly List<AutoResolveEngagementReport> mLastEngagements = new List<AutoResolveEngagementReport>();
        private readonly List<AutoResolveAttritionReport> mLastAttritionReports = new List<AutoResolveAttritionReport>();
        private string mLastWinnerDecision = "(not evaluated)";
        private readonly List<string> mNotes = new List<string>();

        // ---- Game state that the strategic layer supplies ----

        public TargetContrastPort ContrastTable { get; set; }
        public ulong SuperCategoryMask { get; set; }               // GameObjectCategoryType "Super"
        public Func<int, bool> IsPlayableFaction { get; set; }      // owner -> Faction->Is_Playable()
        public Func<int, bool> IsHumanPlayer { get; set; }          // owner -> Player->Is_Human()
        public Func<int, int> TechLevelForOwner { get; set; }       // owner -> Player->Get_Tech_Level()

        // Planet terrain (Get_Terrain_Type) and AITerrainEffectiveness.xml. Null terrain = MAP_TYPE_INVALID.
        public string TerrainType { get; set; }
        public Func<string, string, double?> TerrainEffectiveness { get; set; }

        // Land bombing run: the invading player's bomber type.
        public int BombingRunPlayer { get; set; } = -1;
        public AutoResolveObjectType BomberType { get; set; }

        public bool MidTactical { get; set; }
        public float TacticalMultiplier { get; set; } = 1.0f;
        public float LoserAttrition { get; set; } = 0.35f;
        public float WinnerAttrition { get; set; } = 0.15f;
        public float RetreatLoserAttrition { get; set; } = 0.35f;
        public float RetreatWinnerAttrition { get; set; } = 0.15f;
        public float AttritionAllowanceFactor { get; set; } = 0.333333f;
        public float TransportLosses { get; set; } = 0.333333f;

        // SyncRandom. The live game seed cannot be known from XML, so outcomes that depend on the random
        // attrition order reproduce the game only for a matching seed.
        public AutoResolveRandom SyncRandom { get; set; } = new AutoResolveRandom(1);

        public Func<ulong, string> CategoryNameProvider { get; set; }

        public AutoResolveClass()
        {
            for (int i = 0; i < MAX_HISTORY; i++) mBattleHistory[i] = new AutoResolveBattle();
        }

        // ------------------------------------------------------------------ lifecycle

        public AutoResolveHResult Prepare_For_Space() { return Prepare(true); }
        public AutoResolveHResult Prepare_For_Land() { return Prepare(false); }

        private AutoResolveHResult Prepare(bool space)
        {
            if (mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            mIsCombatPrepared = true;
            mIsCombatInitiated = false;
            mIsSpace = space;
            mRetreatingPlayer = -1;
            mWinningPlayer = -1;
            mRetreatInProgress = false;
            mBattleFought = false;
            mSides[0].Init();
            mSides[1].Init();
            mLastEngagements.Clear();
            mLastAttritionReports.Clear();
            mNotes.Clear();
            mBattleID = (mBattleID + 1) % MAX_HISTORY;
            mBattleHistory[mBattleID] = new AutoResolveBattle();
            return AutoResolveHResult.S_OK;
        }

        // Add_Combatant -> Add_Individual -> Owner_Enters_Fray -> SideStruct::Add_Combatant.
        // Fleets are expected to be expanded by the caller, in fleet order.
        public AutoResolveHResult Add_Combatant(AutoResolveCombatant combatant)
        {
            if (!mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            if (mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_COMBAT_STARTED;
            if (combatant == null || combatant.Type == null) return AutoResolveHResult.E_POINTER;

            AutoResolveHResult result = Owner_Enters_Fray(combatant.OwnerId);
            if (result != AutoResolveHResult.S_OK) return result;

            for (int index = 0; index < mSides.Length; ++index)
            {
                if (mSides[index].OwnerId == combatant.OwnerId) return mSides[index].Add_Combatant(combatant);
            }
            return AutoResolveHResult.S_OK;
        }

        public AutoResolveHResult Initiate_Combat(int aggressor)
        {
            if (mIsCombatInitiated || !mIsCombatPrepared) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;
            mIsCombatInitiated = true;
            mAggressor = aggressor;

            if (Get_Side_By_Owner(aggressor) < 0) return AutoResolveHResult.E_AUTORESOLVE_AGGRESSOR_ERROR;

            for (int index = 0; index < mSides.Length; ++index)
            {
                if (mSides[index].OwnerId == -1) return AutoResolveHResult.E_AUTORESOLVE_NO_CONFLICT;
                if (mSides[index].Queue.Count == 0) return AutoResolveHResult.E_AUTORESOLVE_NO_CONFLICT;
            }

            if (ContrastTable == null) ContrastTable = TargetContrastPort.Build(new List<KeyValuePair<ulong, List<TargetContrastPort.WeightedEntry>>>());

            for (int index = 0; index < mSides.Length; ++index)
            {
                AutoResolveCombatant weakest = null;
                mSides[index].TotalForce = Calculate_Side_Force(mSides[index].Queue, mSides[index].OwnerId, ref weakest);
                mSides[index].WeakestUnit = weakest;
                Find_Special_Heroes(index);
            }

            bool foundSuperWeapon = mSides[0].SuperWeapon != null || mSides[1].SuperWeapon != null;
            if (!foundSuperWeapon && mIsSpace &&
                mSides[0].TotalForce[GLOBAL_SPACE_INDEX].Force <= 0.0 && mSides[1].TotalForce[GLOBAL_SPACE_INDEX].Force <= 0.0)
            {
                // Only transports on both sides.
                int loser;
                int winner;
                if (mSides[0].Queue.Count > mSides[1].Queue.Count) { loser = 1; winner = 0; }
                else if (mSides[0].Queue.Count == mSides[1].Queue.Count)
                {
                    loser = mAggressor == mSides[0].OwnerId ? 0 : 1;
                    winner = loser == 0 ? 1 : 0;
                }
                else { loser = 0; winner = 1; }

                mWinningPlayer = mSides[winner].OwnerId;
                Player_Retreats(mSides[loser].OwnerId);
                mBattleFought = true;
                mLastWinnerDecision = "Only transports on both sides: larger queue wins (tie: aggressor loses); loser retreats.";
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

        // Combat_Round(instant = true). The display timer only delays when results are revealed.
        public AutoResolveHResult Combat_Round(bool instant)
        {
            if (!mIsCombatInitiated) return AutoResolveHResult.E_AUTORESOLVE_NOT_READY;

            if (!mBattleFought)
            {
                float loserAttritionValue = mRetreatInProgress ? RetreatLoserAttrition : LoserAttrition;
                float winnerAttritionValue = mRetreatInProgress ? RetreatWinnerAttrition : WinnerAttrition;

                mLastEngagements.Clear();
                mLastAttritionReports.Clear();

                TargetResult[] results = new TargetResult[2];
                results[1] = Side_Attack(mSides[0].Queue, mSides[1].TotalForce, mSides[0].OwnerId);
                results[0] = Side_Attack(mSides[1].Queue, mSides[0].TotalForce, mSides[1].OwnerId);

                int winner = Determine_Winner_Index(results[0], results[1]);
                int loser = winner == 0 ? 1 : 0;

                bool piratePlayer = !Is_Playable(mSides[loser].OwnerId);

                mWinningPlayer = mSides[winner].OwnerId;

                AutoResolveCombatant wobj = mSides[winner].Queue.Count > 0 ? mSides[winner].Queue[0] : null;
                AutoResolveCombatant lobj = mSides[loser].Queue.Count > 0 ? mSides[loser].Queue[0] : null;

                if (Apply_Transport_Losses(mSides[loser].Queue, piratePlayer, wobj))
                {
                    Player_Retreats(mSides[loser].OwnerId);
                }

                int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
                results[loser][globalIndex].Force += (mSides[loser].TotalForce[globalIndex].Force - results[loser][globalIndex].Force) *
                    (1.0f - loserAttritionValue);
                results[winner][globalIndex].Force += (mSides[winner].TotalForce[globalIndex].Force - results[winner][globalIndex].Force) *
                    (1.0f - winnerAttritionValue);

                mSides[loser].WeakestUnit = null;
                if (piratePlayer)
                {
                    results[loser][globalIndex].Force = 0.0;
                }

                for (int i = 0; i < mSides.Length; i++)
                {
                    AutoResolveCombatant weakest = mSides[i].WeakestUnit;
                    bool anyLeft = Apply_Attrition(mSides[i].Queue, results[i], ref weakest, i == loser, i, i == loser ? wobj : lobj);
                    mSides[i].WeakestUnit = weakest;

                    if (anyLeft && i == loser) Player_Retreats(mSides[i].OwnerId);
                }

                if (mSides[loser].Queue.Count == 0 && mRetreatInProgress)
                {
                    mRetreatInProgress = false;
                    mRetreatingPlayer = -1;
                }

                if (mSides[loser].SuperWeapon != null && mSides[winner].SuperWeaponKiller != null)
                {
                    mNotes.Add("Super weapon destroyed by a super weapon killer: galactic victory condition would be registered.");
                }

                mBattleFought = true;
            }

            return mRetreatInProgress ? AutoResolveHResult.S_AUTORESOLVE_COMBAT_RETREAT : AutoResolveHResult.S_AUTORESOLVE_COMBAT_OVER;
        }

        public AutoResolveHResult Cleanup_Combat()
        {
            mSides[0].Init();
            mSides[1].Init();
            mRetreatInProgress = false;
            mIsCombatPrepared = false;
            mIsCombatInitiated = false;
            return AutoResolveHResult.S_OK;
        }

        public int Who_Won() { return mWinningPlayer; }
        public int Side_Is_Retreating() { return mRetreatingPlayer; }
        public string Get_Last_Winner_Decision() { return mLastWinnerDecision; }
        public List<string> Get_Notes() { return new List<string>(mNotes); }
        public List<AutoResolveEngagementReport> Get_Last_Engagements() { return new List<AutoResolveEngagementReport>(mLastEngagements); }
        public List<AutoResolveAttritionReport> Get_Last_Attrition_Reports() { return new List<AutoResolveAttritionReport>(mLastAttritionReports); }
        public IReadOnlyList<ulong> Get_Contrast_Slot_Order() { return ContrastTable == null ? new List<ulong>() : ContrastTable.ContrastTypeList; }

        public AutoResolveBattle Get_Current_Battle_History()
        {
            AutoResolveBattle battle = new AutoResolveBattle();
            battle.Planet = mBattleHistory[mBattleID].Planet;
            battle.Killed.AddRange(mBattleHistory[mBattleID].Killed);
            return battle;
        }

        // Survivors of each side after the round (the queue after Apply_Attrition).
        public List<AutoResolveCombatant> Get_Queue(int owner)
        {
            int side = Get_Side_By_Owner(owner);
            return side < 0 ? new List<AutoResolveCombatant>() : new List<AutoResolveCombatant>(mSides[side].Queue);
        }

        public void Kill_Retreating_Units()
        {
            if (mRetreatingPlayer == -1) return;
            int winner = mSides[0].OwnerId == mRetreatingPlayer ? 1 : 0;
            int loser = mSides[1].OwnerId == mRetreatingPlayer ? 1 : 0;
            if (loser == winner) return;
            if (mSides[winner].Queue.Count == 0) return;

            foreach (AutoResolveCombatant unit in mSides[loser].Queue)
            {
                Take_Damage(unit);
                Update_Battle_History(unit);
            }

            mRetreatInProgress = false;
            mRetreatingPlayer = -1;
        }

        // ------------------------------------------------------------------ force calculation

        // Calculate_Side_Force (x64 0x140022525).
        private TargetResult Calculate_Side_Force(List<AutoResolveCombatant> units, int playerId, ref AutoResolveCombatant weakestUnit)
        {
            IReadOnlyList<ulong> contrastTypes = ContrastTable.ContrastTypeList;
            TargetResult result = TargetResult.Create(contrastTypes.Count + 2);
            result[GLOBAL_GROUND_INDEX].Ground = true;
            result[GLOBAL_SPACE_INDEX].Ground = false;

            float weakestVal = BIG_FLOAT;
            int techLevel = Owner_Tech_Level(playerId);
            int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;

            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                AutoResolveObjectType type = Get_Type_From_Combatant(unit);

                // x64: Calculate_Side_Force additionally swaps in the type's own unique container unit.
                // Side_Attack and Apply_Attrition do not.
                AutoResolveObjectType container = mIsSpace ? type.UniqueSpaceContainerUnit : type.UniqueGroundContainerUnit;
                if (container != null) type = container;

                if (!mIsSpace && unit.IsPlanet)
                {
                    // Turrets give ground base strength. Note: the slot's Category is not set here.
                    foreach (AutoResolveObjectType built in unit.PlanetBuiltObjects)
                    {
                        if (built == null) continue;
                        int cidx = 2;
                        foreach (ulong category in contrastTypes)
                        {
                            if ((built.CategoryMask & category) != 0UL)
                            {
                                result[cidx].Force += built.AICombatPower;
                                result[GLOBAL_GROUND_INDEX].Force += built.AICombatPower;
                                break;
                            }
                            cidx++;
                        }
                    }
                    continue;
                }

                // Garrison (spawned) units. Note: the slot's Category is not set here either.
                if (Should_Add_Garrison(unit))
                {
                    foreach (AutoResolveSpawnedUnit spawned in type.Get_Spawned_Units(techLevel))
                    {
                        if (spawned.Type == null) continue;
                        int cidx = 2;
                        foreach (ulong category in contrastTypes)
                        {
                            if ((spawned.Type.CategoryMask & category) != 0UL)
                            {
                                float totalForce = type.Get_Spawned_Unit_Starting_Count(techLevel, spawned.Type) * spawned.Type.AICombatPower;
                                result[cidx].Force += totalForce;
                                result[globalIndex].Force += totalForce;
                                break;
                            }
                            cidx++;
                        }
                    }
                }

                if (type.IsTransport && mIsSpace) continue;

                if (type.AICombatPower < weakestVal)
                {
                    weakestUnit = unit;
                    weakestVal = type.AICombatPower;
                }

                // Every slot up to and including the first match gets its Category/Ground set. A slot that
                // no unit walks past keeps Category 0, which Find_Contrast_Index skips as a target and
                // Apply_Unit_Contrast treats like a global slot.
                int slot = 2;
                foreach (ulong category in contrastTypes)
                {
                    result[slot].Category = category;
                    result[slot].Ground = !mIsSpace;
                    if ((type.CategoryMask & category) != 0UL)
                    {
                        result[slot].Force += type.AICombatPower;
                        result[globalIndex].Force += type.AICombatPower;
                        break;
                    }
                    slot++;
                }
            }

            return result;
        }

        private static bool Contains_Super_Weapon_Killer(AutoResolveObjectType type)
        {
            foreach (AutoResolveObjectType member in type.GroundCompanyUnits) if (member != null && member.IsSuperWeaponKiller) return true;
            foreach (AutoResolveObjectType member in type.SquadronUnits) if (member != null && member.IsSuperWeaponKiller) return true;
            return false;
        }

        private void Find_Special_Heroes(int index)
        {
            SideStruct side = mSides[index];
            foreach (AutoResolveCombatant combatant in side.Queue)
            {
                AutoResolveObjectType otype = combatant.IsPlanet ? combatant.PlanetBaseType : combatant.Type;
                if (otype == null) continue;
                if (Is_Super_Weapon(otype)) side.SuperWeapon = combatant;
                if (Contains_Super_Weapon_Killer(otype)) side.SuperWeaponKiller = combatant;
            }
        }

        // ------------------------------------------------------------------ attack

        // Side_Attack (x64 0x140054962).
        private TargetResult Side_Attack(List<AutoResolveCombatant> units, TargetResult targetForce, int playerId)
        {
            TargetResult result = targetForce.Clone();
            int techLevel = Owner_Tech_Level(playerId);

            // Hero special-ability strength factors, indexed by lowest category bit (32 entries).
            float[] catTable = new float[32];
            foreach (AutoResolveCombatant unit in units)
            {
                AutoResolveObjectType type = Get_Type_From_Combatant(unit);
                if (!type.HasSpecialAbility) continue;
                foreach (KeyValuePair<ulong, float> factor in type.SpecialAbilityUnitStrengthFactors)
                {
                    float weight = factor.Value - 1.0f;
                    int idx = Get_First_Bit_Set(factor.Key);
                    if (idx > -1 && idx < catTable.Length && weight > catTable[idx]) catTable[idx] = weight;
                }
            }

            result[GLOBAL_GROUND_INDEX].Ground = true;
            result[GLOBAL_SPACE_INDEX].Ground = false;

            if (BombingRunPlayer == playerId && BomberType != null)
            {
                int best = -1;
                float power = BomberType.AICombatPower;
                while (power > 0.0f && best != 0)
                {
                    Attack_With(ref power, BomberType, result, ref best, catTable, playerId, "Bombing run", -1);
                    best = 0;
                }
            }

            for (int i = 0; i < units.Count; i++)
            {
                AutoResolveCombatant unit = units[i];
                AutoResolveObjectType type = Get_Type_From_Combatant(unit);

                // One best_category per combatant is shared by its built objects, every garrison instance
                // and the unit itself. Each while loop sets it to 0 after one pass, so only the FIRST of
                // those with power > 0 actually attacks; the rest are skipped.
                int best = -1;

                if (!mIsSpace && unit.IsPlanet)
                {
                    foreach (AutoResolveObjectType built in unit.PlanetBuiltObjects)
                    {
                        if (built == null) continue;
                        float power = built.AICombatPower;
                        while (power > 0.0f && best != 0)
                        {
                            Attack_With(ref power, built, result, ref best, catTable, playerId, "Built object of " + unit.TypeName, i);
                            best = 0;
                        }
                    }
                    continue;
                }

                if (Should_Add_Garrison(unit))
                {
                    foreach (AutoResolveSpawnedUnit spawned in type.Get_Spawned_Units(techLevel))
                    {
                        if (spawned.Type == null) continue;
                        int activeCount = type.Get_Spawned_Unit_Starting_Count(techLevel, spawned.Type);
                        for (int k = 0; k < activeCount; ++k)
                        {
                            float power = spawned.Type.AICombatPower;
                            while (power > 0.0f && best != 0)
                            {
                                Attack_With(ref power, spawned.Type, result, ref best, catTable, playerId, "Garrison " + spawned.Type.Name + " of " + unit.TypeName, i);
                                best = 0;
                            }
                        }
                    }
                }

                if (type.IsTransport && mIsSpace) continue;

                float unitPower = type.AICombatPower;
                while (unitPower > 0.0f && best != 0)
                {
                    Attack_With(ref unitPower, type, result, ref best, catTable, playerId, "Unit", i);
                    best = 0;
                }
            }

            return result;
        }

        private void Attack_With(ref float remainingPower, AutoResolveObjectType type, TargetResult result, ref int best,
            float[] catTable, int playerId, string kind, int unitIndex)
        {
            AutoResolveEngagementReport report = new AutoResolveEngagementReport();
            report.AttackerOwnerId = playerId;
            report.SourceTypeName = type.Name;
            report.SourceUnitIndex = unitIndex;
            report.SourceKind = kind;
            report.SourcePowerBefore = remainingPower;
            report.SourceCategory = Category_Name(type.CategoryMask);

            Find_Contrast_Index(remainingPower, type, result, ref best);

            int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
            report.TargetCategory = best > 0 ? Category_Name(result[best].Category) : "(global)";
            int targetIndex = best > 0 ? best : globalIndex;
            report.TargetCategoryBefore = result[targetIndex].Force;
            report.TargetGlobalBefore = result[globalIndex].Force;

            Apply_Unit_Contrast(ref remainingPower, type, result, best, catTable, report);

            report.TargetCategoryAfter = result[targetIndex].Force;
            report.TargetGlobalAfter = result[globalIndex].Force;
            report.SourcePowerAfter = remainingPower;
            mLastEngagements.Add(report);
        }

        // Find_Contrast_Index (x64 0x1400493af): computed in double. best_category is only written when a
        // candidate beats the running best, so it stays -1 when nothing qualifies.
        private void Find_Contrast_Index(float remainingPower, AutoResolveObjectType type, TargetResult current, ref int bestCategory)
        {
            double bestWeight = 0.0;
            for (int i = 0; i < current.Count; ++i)
            {
                if (current[i].Category == 0UL) continue;
                double remaining = current[i].Force;
                if (!(0.0 < remaining)) continue;

                double contrastWeight = ContrastTable.Get_Average_Contrast_Factor(type, current[i].Category);
                if (TerrainType != null)
                {
                    contrastWeight *= Get_Effectiveness_On_Terrain(type);
                }
                if (!(0.0 < contrastWeight)) continue;

                double scaled = (double)remainingPower * contrastWeight;
                double buildWeight = (remaining - scaled) / Math.Max(remaining, scaled);
                buildWeight = Math.Max(buildWeight * -buildWeight + 1.0, 0.0);
                if (bestWeight < buildWeight * contrastWeight)
                {
                    bestCategory = i;
                    bestWeight = buildWeight * contrastWeight;
                }
            }
        }

        // Apply_Unit_Contrast (x64 0x14006cae9).
        private void Apply_Unit_Contrast(ref float remainingPower, AutoResolveObjectType type, TargetResult current, int bestCategory,
            float[] factorTable, AutoResolveEngagementReport report)
        {
            float originalPower = remainingPower;

            // Hero factor. Note the quirk: the table value is compared against the running factor,
            // which already includes the +1.
            ulong ctype = type.CategoryMask;
            int cval = Get_First_Bit_Set(ctype);
            float factor = 0.0f;
            while (cval > -1)
            {
                float tableValue = cval < factorTable.Length ? factorTable[cval] : 0.0f;
                if (factor < tableValue) factor = tableValue + 1.0f;
                ctype &= ~(1UL << cval);
                cval = Get_First_Bit_Set(ctype);
            }
            if (factor != 0.0f) remainingPower *= factor;
            report.HeroMultiplier = factor != 0.0f ? factor : 1.0f;

            if (bestCategory < 1)
            {
                // No target category: the power goes straight into the first global slot whose Ground
                // matches the ATTACKER (ground = has company units or is a dummy ground base), which can be
                // the wrong theatre's slot for non-company land types.
                bool ground = type.GroundCompanyUnits.Count > 0 || type.IsDummyGroundBase;
                for (int i = 0; i < current.Count; ++i)
                {
                    if (current[i].Category == 0UL && current[i].Ground == ground)
                    {
                        current[i].Force -= remainingPower;
                        break;
                    }
                }
                report.ScaledPower = remainingPower;
                report.AppliedCombatPower = remainingPower;
                report.ContrastMultiplier = 1.0f;
                report.TotalMultiplier = report.HeroMultiplier;
                remainingPower = 0.0f;
                return;
            }

            float contrastWeight = (float)ContrastTable.Get_Average_Contrast_Factor(type, current[bestCategory].Category);
            if (current[bestCategory].Ground && TerrainType != null)
            {
                contrastWeight *= (float)Get_Effectiveness_On_Terrain(type);
            }
            remainingPower *= contrastWeight;
            report.ScaledPower = remainingPower;
            report.ContrastMultiplier = contrastWeight;
            report.TotalMultiplier = report.HeroMultiplier * contrastWeight;

            float modifiedForceApplied = Math.Min((float)current[bestCategory].Force, remainingPower);
            remainingPower = (1.0f - modifiedForceApplied / remainingPower) * originalPower;
            current[bestCategory].Force -= modifiedForceApplied;
            report.AppliedCombatPower = modifiedForceApplied;

            for (int i = 0; i < current.Count; ++i)
            {
                if (current[i].Category == 0UL && current[i].Ground == current[bestCategory].Ground)
                {
                    current[i].Force -= (double)(modifiedForceApplied + remainingPower);
                    break;
                }
            }
        }

        // TargetContrastClass::Get_Effectiveness_On_Terrain: first company unit's movement class for
        // companies, otherwise the type's own; 1.0 when the table has no entry.
        private double Get_Effectiveness_On_Terrain(AutoResolveObjectType type)
        {
            if (TerrainEffectiveness == null || TerrainType == null) return 1.0;
            string movement = type.GroundCompanyUnits.Count > 0 && type.GroundCompanyUnits[0] != null
                ? type.GroundCompanyUnits[0].MovementClass
                : type.MovementClass;
            double? value = TerrainEffectiveness(TerrainType, movement);
            return value.HasValue ? value.Value : 1.0;
        }

        // Determine_Winner_Index (x64 0x14002cb88): totals are doubles and include every positive slot
        // (global and category slots alike).
        private int Determine_Winner_Index(TargetResult resultsA, TargetResult resultsB)
        {
            mLastWinnerDecision = "";
            // Death Star hack. The super weapon always wins unless the opposition has a killer.
            if (mSides[0].SuperWeapon != null && mSides[1].SuperWeaponKiller == null)
            {
                Player_Retreats(mSides[1].OwnerId);
                mLastWinnerDecision = "Side A has a super weapon and side B has no super weapon killer, so side B retreats. ";
            }
            else if (mSides[1].SuperWeapon != null && mSides[0].SuperWeaponKiller == null)
            {
                Player_Retreats(mSides[0].OwnerId);
                mLastWinnerDecision = "Side B has a super weapon and side A has no super weapon killer, so side A retreats. ";
            }

            if (mRetreatInProgress)
            {
                mLastWinnerDecision += "Winner is the side that is not retreating.";
                return mRetreatingPlayer == mSides[0].OwnerId ? 1 : 0;
            }

            double totalA = 0.0;
            bool anyPositiveA = false;
            for (int i = 0; i < resultsA.Count; ++i)
            {
                if (0.0 < resultsA[i].Force) { anyPositiveA = true; totalA += resultsA[i].Force; }
            }
            double totalB = 0.0;
            bool anyPositiveB = false;
            for (int i = 0; i < resultsB.Count; ++i)
            {
                if (0.0 < resultsB[i].Force) { anyPositiveB = true; totalB += resultsB[i].Force; }
            }

            string totals = "A=" + totalA.ToString("0.###", CultureInfo.InvariantCulture) + " vs B=" + totalB.ToString("0.###", CultureInfo.InvariantCulture);
            if (anyPositiveA && anyPositiveB)
            {
                bool human0 = Is_Human(mSides[0].OwnerId);
                bool human1 = Is_Human(mSides[1].OwnerId);
                if (human0 || human1)
                {
                    mLastWinnerDecision += "Both sides have remaining force and a human is involved: larger total wins, ties go to side B (" + totals + ").";
                    return totalA > totalB ? 0 : 1;
                }
                if (!Is_Playable(mSides[0].OwnerId))
                {
                    mLastWinnerDecision += "AI vs AI with force left: side A's faction is not playable, so side B wins.";
                    return 1;
                }
                if (!Is_Playable(mSides[1].OwnerId))
                {
                    mLastWinnerDecision += "AI vs AI with force left: side B's faction is not playable, so side A wins.";
                    return 0;
                }
                mLastWinnerDecision += "AI vs AI, both playable: larger total wins, ties go to side B (" + totals + ").";
                return totalA > totalB ? 0 : 1;
            }
            if ((anyPositiveA || anyPositiveB) && totalA != totalB)
            {
                mLastWinnerDecision += "Only one side has force left (" + totals + ").";
                return totalA > totalB ? 0 : 1;
            }

            mLastWinnerDecision += "Neither side has force left: the aggressor wins.";
            return mAggressor == mSides[0].OwnerId ? 0 : 1;
        }

        // ------------------------------------------------------------------ losses

        private bool Apply_Transport_Losses(List<AutoResolveCombatant> units, bool isPirate, AutoResolveCombatant killer)
        {
            if (!mIsSpace) return false;

            int tcnt = 0;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Type.IsTransport) tcnt++;
            }

            int rcnt = tcnt == 1 ? 0 : (int)(((float)tcnt) * (1.0f - TransportLosses) + 0.5f);
            if (isPirate) rcnt = 0;

            tcnt = 0;
            bool anyLeft = false;

            // Transports carrying named heroes escape first and are removed from the queue.
            for (int i = 0; i < units.Count && tcnt < rcnt; i++)
            {
                if (units[i].Type.IsTransport && Contains_Named_Hero(units[i]))
                {
                    ++tcnt;
                    Add_Attrition_Report(units[i], -1, false, units[i].Type.AICombatPower, 0, 0, "Escapes", "Transport carrying a named hero escapes the battle.");
                    units.RemoveAt(i);
                    --i;
                    anyLeft = true;
                }
            }

            // Destroyed transports stay in the queue; in space Apply_Attrition then keeps them as
            // left-overs, so they still count as "survivors" that force a retreat.
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].Type.IsTransport) continue;
                if (tcnt >= rcnt || isPirate)
                {
                    Update_Battle_History(units[i]);
                    Take_Damage(units[i]);
                    Add_Attrition_Report(units[i], -1, false, units[i].Type.AICombatPower, 0, 0, "KillUnit", "Transport lost (transport losses).");
                }
                else
                {
                    anyLeft = true;
                    Add_Attrition_Report(units[i], -1, false, units[i].Type.AICombatPower, 0, 0, "KeepUnit", "Transport survives (transport losses).");
                }
                tcnt++;
            }
            return anyLeft;
        }

        // Apply_Attrition (x64 0x14004f57f).
        private bool Apply_Attrition(List<AutoResolveCombatant> units, TargetResult current, ref AutoResolveCombatant weakestUnit,
            bool isLoser, int index, AutoResolveCombatant killer)
        {
            float allowance = AttritionAllowanceFactor;
            bool weakKilled = false;
            int globalIndex = mIsSpace ? GLOBAL_SPACE_INDEX : GLOBAL_GROUND_INDEX;
            int sideOwner = mSides[index].OwnerId;
            int techLevel = Owner_Tech_Level(sideOwner);

            if (MidTactical)
            {
                current[globalIndex].Force *= TacticalMultiplier;
            }

            List<AutoResolveCombatant> leftOvers = new List<AutoResolveCombatant>();

            while (units.Count != 0)
            {
                int unitIndex = -1;
                bool isStructure = false;

                // Named heroes and dummy ground structures are handled first, otherwise a random unit.
                int scan;
                for (scan = 0; scan < units.Count; ++scan)
                {
                    AutoResolveCombatant candidate = units[scan];
                    if (Contains_Named_Hero(candidate)) { unitIndex = scan; break; }
                    if (candidate.Type.IsDummyGroundStructure) { unitIndex = scan; isStructure = true; break; }
                }

                if (unitIndex == -1) unitIndex = SyncRandom.Get(0, units.Count - 1);

                AutoResolveCombatant unit = units[unitIndex];
                AutoResolveObjectType type = Get_Type_From_Combatant(unit);
                bool killUnit = false;
                bool killBase = false;
                double before = current[globalIndex].Force;
                string decision = "KeepUnit";
                string notes = "";

                if (!mIsSpace || !unit.Type.IsTransport)
                {
                    if (isLoser && !Is_Playable(unit.OwnerId) && !unit.IsPlanet)
                    {
                        killUnit = true;
                        notes = "Non-playable factions are wiped out when they lose.";
                    }
                    else if (Is_Super_Weapon(type))
                    {
                        killUnit = false;
                        notes = "Super weapons survive.";
                        if (isLoser && mSides[index == 0 ? 1 : 0].SuperWeaponKiller != null)
                        {
                            killUnit = true;
                            notes = "Super weapon destroyed by the opposing super weapon killer.";
                            // The source compares weakest_unit with units[i], where i is the hero/structure
                            // scan index left over from the loop above (units.size() when nothing was found).
                            if (scan < units.Count && ReferenceEquals(weakestUnit, units[scan])) weakestUnit = null;
                        }
                    }
                    else if (isLoser && unit.IsPlanet)
                    {
                        Update_Battle_History(unit);
                        killBase = true;
                        killUnit = false;
                        notes = "The loser's base is always destroyed.";
                    }
                    else if (isLoser && isStructure)
                    {
                        killUnit = true;
                        notes = "The loser's structures are always destroyed.";
                    }
                    else
                    {
                        killUnit = true;

                        if (Should_Add_Garrison(unit))
                        {
                            foreach (AutoResolveSpawnedUnit spawned in type.Get_Spawned_Units(techLevel))
                            {
                                if (spawned.Type == null) continue;
                                float totalForce = type.Get_Spawned_Unit_Starting_Count(techLevel, spawned.Type) * spawned.Type.AICombatPower;
                                current[globalIndex].Force -= totalForce;
                                current[globalIndex].Force = Math.Max(current[globalIndex].Force, 0.0);
                            }
                        }

                        if (unit.IsPlanet)
                        {
                            if (mIsSpace)
                            {
                                AutoResolveObjectType newType = unit.BaseTypeForCombatRating != null
                                    ? unit.BaseTypeForCombatRating(current[globalIndex].Force, allowance)
                                    : type;
                                if (newType != null)
                                {
                                    current[globalIndex].Force -= newType.AICombatPower;
                                    notes = "Base kept at level " + newType.Name + ".";
                                }
                                else
                                {
                                    killBase = true;
                                    notes = "Base destroyed.";
                                }
                            }
                            killUnit = false;
                        }
                        else if (0.0 < current[globalIndex].Force - (double)(type.AICombatPower * allowance))
                        {
                            current[globalIndex].Force -= type.AICombatPower;
                            current[globalIndex].Force = Math.Max(current[globalIndex].Force, 0.0);
                            killUnit = false;
                            notes = "Remaining force covers this unit.";
                        }
                        else
                        {
                            notes = "Remaining force does not cover this unit.";
                        }
                    }
                }
                else
                {
                    notes = "Space transports are not subject to attrition.";
                }

                if (killUnit)
                {
                    if (!ReferenceEquals(weakestUnit, unit))
                    {
                        Take_Damage(unit);
                        Update_Battle_History(unit);
                        decision = "KillUnit";
                    }
                    else
                    {
                        weakKilled = true;
                        decision = "KillDeferred";
                        notes += " Weakest unit: killed only if other units survive.";
                    }
                }
                else if (killBase)
                {
                    decision = "KillBase";
                }
                else
                {
                    leftOvers.Add(unit);
                }

                Add_Attrition_Report(unit, index, isLoser, type.AICombatPower, before, current[globalIndex].Force, decision, notes);
                units.RemoveAt(unitIndex);
            }

            units.AddRange(leftOvers);
            bool anySurvivors = leftOvers.Count != 0;

            if (anySurvivors && weakestUnit != null && weakKilled)
            {
                Take_Damage(weakestUnit);
                Update_Battle_History(weakestUnit);
                Add_Attrition_Report(weakestUnit, index, isLoser, weakestUnit.Type.AICombatPower, current[globalIndex].Force, current[globalIndex].Force,
                    "KillUnit", "Deferred weakest unit destroyed because other units survived.");
            }

            if (!anySurvivors && !isLoser && weakestUnit != null)
            {
                units.Add(weakestUnit);
                Add_Attrition_Report(weakestUnit, index, isLoser, weakestUnit.Type.AICombatPower, current[globalIndex].Force, current[globalIndex].Force,
                    "KeepUnit", "Winner keeps its weakest unit when nothing else survives.");
                return true;
            }

            return anySurvivors;
        }

        // ------------------------------------------------------------------ helpers

        // Get_Type_From_Combatant (x64 0x14006c8d7).
        private AutoResolveObjectType Get_Type_From_Combatant(AutoResolveCombatant combatant)
        {
            if (combatant.IsPlanet) return combatant.PlanetBaseType ?? combatant.Type;

            AutoResolveObjectType current = combatant.CurrentType ?? combatant.Type;
            AutoResolveObjectType type = combatant.Type;

            if (type.IsTransport)
            {
                foreach (AutoResolveObjectType landUnit in Transport_Contents(combatant))
                {
                    if (landUnit == null) continue;
                    if (mIsSpace && landUnit.UniqueSpaceContainerUnit != null) { type = landUnit.UniqueSpaceContainerUnit; break; }
                    if (!mIsSpace && landUnit.UniqueGroundContainerUnit != null) { type = landUnit.UniqueGroundContainerUnit; break; }
                }
            }
            else
            {
                // x64: non-transports also check their company units for a unique container unit.
                foreach (AutoResolveObjectType member in combatant.Type.GroundCompanyUnits)
                {
                    if (member == null) continue;
                    AutoResolveObjectType container = mIsSpace ? member.UniqueSpaceContainerUnit : member.UniqueGroundContainerUnit;
                    if (container != null) { type = container; break; }
                }
            }

            // x64: keep the stronger of the substituted type and the object's current type.
            if (current != null && !ReferenceEquals(current, type) && type.AICombatPower < current.AICombatPower)
            {
                type = current;
            }
            return type;
        }

        private static List<AutoResolveObjectType> Transport_Contents(AutoResolveCombatant combatant)
        {
            return combatant.TransportContents ?? combatant.Type.GroundCompanyUnits;
        }

        // GameObjectClass::Contains_Named_Hero: the type itself, or anything inside a transport.
        private static bool Contains_Named_Hero(AutoResolveCombatant combatant)
        {
            if (combatant.Type.IsNamedHero) return true;
            if (!combatant.Type.IsTransport) return false;
            foreach (AutoResolveObjectType content in Transport_Contents(combatant))
            {
                if (content != null && content.IsNamedHero) return true;
            }
            return false;
        }

        private bool Is_Super_Weapon(AutoResolveObjectType type)
        {
            return SuperCategoryMask != 0UL && (type.CategoryMask & SuperCategoryMask) != 0UL;
        }

        // add_garrison = (Get_Parent_Mode_ID() == INVALID_OBJECT_ID), except star bases mid-tactical.
        private bool Should_Add_Garrison(AutoResolveCombatant unit)
        {
            if (unit.HasParentModeId) return false;
            AutoResolveObjectType objectType = unit.CurrentType ?? unit.Type;
            if (objectType.IsDummyStarBase && MidTactical) return false;
            return true;
        }

        private bool Is_Playable(int owner) { return IsPlayableFaction == null || IsPlayableFaction(owner); }
        private bool Is_Human(int owner) { return IsHumanPlayer != null && IsHumanPlayer(owner); }
        private int Owner_Tech_Level(int owner) { return TechLevelForOwner == null ? 0 : TechLevelForOwner(owner); }

        private void Take_Damage(AutoResolveCombatant unit) { unit.IsAlive = false; }

        private void Update_Battle_History(AutoResolveCombatant unit)
        {
            mBattleHistory[mBattleID].Killed.Add(new KeyValuePair<string, int>(unit.TypeName, unit.OwnerId));
        }

        private void Add_Attrition_Report(AutoResolveCombatant unit, int sideIndex, bool isLoser, float power, double before, double after, string decision, string notes)
        {
            AutoResolveAttritionReport report = new AutoResolveAttritionReport();
            report.SideOwnerId = unit.OwnerId;
            report.SideIndex = sideIndex >= 0 ? sideIndex : Get_Side_By_Owner(unit.OwnerId);
            report.IsLoserSide = isLoser || (sideIndex < 0);
            report.UnitTypeName = unit.TypeName;
            report.UnitPower = power;
            report.ForceBefore = before;
            report.ForceAfter = after;
            report.Decision = decision;
            report.Notes = notes;
            mLastAttritionReports.Add(report);
        }

        private AutoResolveHResult Owner_Enters_Fray(int owner)
        {
            for (int index = 0; index < mSides.Length; ++index)
            {
                if (mSides[index].OwnerId == owner) return AutoResolveHResult.S_OK;
                if (mSides[index].OwnerId == -1)
                {
                    mSides[index].OwnerId = owner;
                    return AutoResolveHResult.S_OK;
                }
            }
            return AutoResolveHResult.E_AUTORESOLVE_TOO_MANY_SIDES;
        }

        private int Get_Side_By_Owner(int owner)
        {
            for (int index = 0; index < mSides.Length; ++index)
            {
                if (mSides[index].OwnerId == owner) return index;
            }
            return -1;
        }

        private static int Get_First_Bit_Set(ulong value)
        {
            if (value == 0UL) return -1;
            int bit = 0;
            while ((value & 1UL) == 0UL) { value >>= 1; bit++; }
            return bit;
        }

        private string Category_Name(ulong mask)
        {
            if (mask == 0UL) return "(none)";
            if (CategoryNameProvider != null)
            {
                string name = CategoryNameProvider(mask);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            return "0x" + mask.ToString("X", CultureInfo.InvariantCulture);
        }
    }
}
