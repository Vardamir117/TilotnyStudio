using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Holocron
{
    public partial class UnitSort : Form
    {
        public UnitSortClass sortConfig;
        public int UnitRBtype;
        public string sortDocumentation;
        public bool cancel;

        public enum UnitSortTypes
        {
            Internal,
            Class,
            SType,
            AType,
            ChainStart,
            ChainEnd,
            Name, //Put all string sort classes before Name so < can find them
            Pop,
            Price,
            SkPrice,
            Time,
            SkTime,
            CompanySize,
            Shipyard,
            Crew,
            CP,
            Durability,
            HP,
            Shield,
            Regen,
            Speed,
            Accel,
            Turn,
            dpsRaw,
            dpsAvg,
            dpsArmor,
            dpsShield,
            Complement,
            GarrisonCap,
            GarrisonValue,
            NameCount,
            pdRecharge,
            pdRadius,
            Heal,
            Discount,
            TimeReduction,
            incomePercent,
            incomeAmount,
            CommandBonus,
        }

        public struct UnitSortClass
        {
            public UnitSortTypes SortType;
            public string denomtype;
            public bool Descending;
            public bool Accuracy;
            public bool complementCP;
            public int DurabilityMode;
            public int fighterBomberMode;
            public int upfrontReserveMode;
            public int HealMode;
            public int CommandMode;
            public int CommandTypeMode;
        }

        public UnitSort()
        {
            InitializeComponent();
        }

        private void UserSort_Load(object sender, EventArgs e)
        {
            //Make convert to alternate mode function that turns illegal sorts (normalize by crew on ground) to whatever and can be called from main without opening this as a windows

            List<string> denomTypes = new List<string> {
                "Absolute Value",
                "Per Combat Power",
                "Per Population",
                "Per Credit",
                "Per Skirmish Price",
                "Per Build Time",
                "Per Skirmish Time",
            };

            //These only don't share locations for convenience in layout coding
            ShipyardRB.Location = CompanySizeRB.Location;

            switch (UnitRBtype)
            {
                case 0: //space unit
                    denomTypes.Add("Per Crew");
                    ShipyardRB.Visible = true;
                    CrewRB.Visible = true;
                    //ComplementRB.Visible = true;
                    FighterTypeBox.Visible = true;
                    //ReserveBox.Visible = true; //Think of the XML spawns for heroes and builsings
                    GarrisonSlotsRB.Visible = false;
                    GarrisonValueRB.Visible = false;
                    break;
                case 1: //ground companies
                    denomTypes.Add("Per Unit in Company");
                    CompanySizeRB.Visible = true;
                    break;
                case 2: //ground units
                case 6: //GroundHero
                    denomTypes = new List<string> { "Absolute Value", "Per Combat Power" };
                    PriceRB.Visible = false;
                    SkPriceRB.Visible = false;
                    BuildTimeRB.Visible = false;
                    SkBuildTimeRB.Visible = false;
                    PopRB.Visible = false;
                    NameCountRB.Visible = false;
                    break;
                case 3: //Fighter
                    break;
                case 4: //SpaceHero
                    PriceRB.Visible = false;
                    BuildTimeRB.Visible = false;
                    FighterTypeBox.Visible = true;
                    GarrisonSlotsRB.Visible = false;
                    GarrisonValueRB.Visible = false;
                    break;
                case 5: //HeroCompanies
                    PriceRB.Visible = false;
                    BuildTimeRB.Visible = false;
                    denomTypes.Add("Per Unit in Company");
                    CompanySizeRB.Visible = true;
                    break;
                case 7: //ground structures
                    break;
                case 8: //space structures
                    FighterTypeBox.Visible = true;
                    break;
            }

            DenomComboBox.Items.Clear();
            foreach (string denom in denomTypes) DenomComboBox.Items.Add(denom);

            loadFromConfig();
        }

        private void loadFromConfig()
        {
            switch (sortConfig.SortType)
            {
                case UnitSortTypes.Name:
                    NameRB.Checked = true;
                    break;
                case UnitSortTypes.Pop:
                    PopRB.Checked = true;
                    break;
                case UnitSortTypes.Internal:
                    InternalRB.Checked = true;
                    break;
                case UnitSortTypes.Class:
                    ClassRB.Checked = true;
                    break;
                case UnitSortTypes.Price:
                    PriceRB.Checked = true;
                    break;
                case UnitSortTypes.SkPrice:
                    SkPriceRB.Checked = true;
                    break;
                case UnitSortTypes.Time:
                    BuildTimeRB.Checked = true;
                    break;
                case UnitSortTypes.SkTime:
                    SkBuildTimeRB.Checked = true;
                    break;
                case UnitSortTypes.CompanySize:
                    CompanySizeRB.Checked = true;
                    break;
                case UnitSortTypes.Shipyard:
                    ShipyardRB.Checked = true;
                    break;
                case UnitSortTypes.CP:
                    cpRB.Checked = true;
                    break;
                case UnitSortTypes.Durability:
                    DurabilityRB.Checked = true;
                    break;
                case UnitSortTypes.HP:
                    hpRB.Checked = true;
                    break;
                case UnitSortTypes.Shield:
                    ShieldRB.Checked = true;
                    break;
                case UnitSortTypes.Regen:
                    RegenRB.Checked = true;
                    break;
                case UnitSortTypes.SType:
                    STypeRB.Checked = true;
                    break;
                case UnitSortTypes.AType:
                    ATypeRB.Checked = true;
                    break;
                case UnitSortTypes.Speed:
                    SpeedRB.Checked = true;
                    break;
                case UnitSortTypes.Accel:
                    AccelRB.Checked = true;
                    break;
                case UnitSortTypes.Turn:
                    TurnRB.Checked = true;
                    break;
                case UnitSortTypes.dpsRaw:
                    dpsRawRB.Checked = true;
                    break;
                case UnitSortTypes.dpsAvg:
                    dpsAvgRB.Checked = true;
                    break;
                case UnitSortTypes.dpsArmor:
                    dpsArmorRB.Checked = true;
                    break;
                case UnitSortTypes.dpsShield:
                    dpsShieldRB.Checked = true;
                    break;
                case UnitSortTypes.Complement:
                    ComplementRB.Checked = true;
                    break;
                case UnitSortTypes.GarrisonCap:
                    GarrisonSlotsRB.Checked = true;
                    break;
                case UnitSortTypes.GarrisonValue:
                    GarrisonValueRB.Checked = true;
                    break;
                case UnitSortTypes.NameCount:
                    NameCountRB.Checked = true;
                    break;
                case UnitSortTypes.ChainStart:
                    ChainStartRB.Checked = true;
                    break;
                case UnitSortTypes.ChainEnd:
                    ChainEndRB.Checked = true;
                    break;
                case UnitSortTypes.pdRecharge:
                    pdRechargeRB.Checked = true;
                    break;
                case UnitSortTypes.pdRadius:
                    pdRadiusRB.Checked = true;
                    break;
                case UnitSortTypes.Heal:
                    HealRB.Checked = true;
                    break;
                case UnitSortTypes.Discount:
                    DiscountPercentRB.Checked = true;
                    break;
                case UnitSortTypes.TimeReduction:
                    TimeReductionRB.Checked = true;
                    break;
                case UnitSortTypes.incomePercent:
                    IncomePercentRB.Checked = true;
                    break;
                case UnitSortTypes.incomeAmount:
                    IncomeAmountRB.Checked = true;
                    break;
                case UnitSortTypes.CommandBonus:
                    CommandBonusRB.Checked = true;
                    break;
                default:
                    break;
            }

            //todo = Set RB and such from passed UnitConfig
            DurabilityBox.SelectedIndex = sortConfig.DurabilityMode;
            if (DurabilityBox.SelectedIndex < 0) DurabilityBox.SelectedIndex = 0;
            FighterTypeBox.SelectedIndex = sortConfig.fighterBomberMode;
            if (FighterTypeBox.SelectedIndex < 0) FighterTypeBox.SelectedIndex = 0;
            ReserveBox.SelectedIndex = sortConfig.upfrontReserveMode;
            if (ReserveBox.SelectedIndex < 0) ReserveBox.SelectedIndex = 0;
            DenomComboBox.Text = sortConfig.denomtype;
            if (DenomComboBox.SelectedIndex < 0) DenomComboBox.SelectedIndex = 0;
            HealBox.SelectedIndex = sortConfig.HealMode;
            if (HealBox.SelectedIndex < 0) HealBox.SelectedIndex = 0;
            CommandBox.SelectedIndex = sortConfig.CommandMode;
            if (CommandBox.SelectedIndex < 0) CommandBox.SelectedIndex = 0;
            CommandTypeBox.SelectedIndex = sortConfig.CommandTypeMode;
            if (CommandTypeBox.SelectedIndex < 0) CommandTypeBox.SelectedIndex = 0;
            if (sortConfig.Descending) AscComboBox.SelectedIndex = 1;
            else AscComboBox.SelectedIndex = 0;
            ComplementCheckBox.Checked = sortConfig.complementCP;
            AccuracyCheckBox.Checked = sortConfig.Accuracy;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            foreach(Control rb in this.Controls)
            {
                if (rb.GetType() == typeof(System.Windows.Forms.RadioButton))
                {
                    if (((RadioButton)rb).Checked)
                    {
                        sortDocumentation = rb.Text;
                        break;
                    }
                }
            }
            //todo invert this list on form load. Or better, refactor into a loop that sets rb.tag?
            if (NameRB.Checked) sortConfig.SortType = UnitSortTypes.Name;
            else if (PopRB.Checked) sortConfig.SortType = UnitSortTypes.Pop;
            else if (InternalRB.Checked) sortConfig.SortType = UnitSortTypes.Internal;
            else if (ClassRB.Checked) sortConfig.SortType = UnitSortTypes.Class;
            else if (PriceRB.Checked) sortConfig.SortType = UnitSortTypes.Price;
            else if (SkPriceRB.Checked) sortConfig.SortType = UnitSortTypes.SkPrice;
            else if (BuildTimeRB.Checked) sortConfig.SortType = UnitSortTypes.Time;
            else if (SkBuildTimeRB.Checked) sortConfig.SortType = UnitSortTypes.SkTime;
            else if (CompanySizeRB.Checked) sortConfig.SortType = UnitSortTypes.CompanySize;
            else if (ShipyardRB.Checked) sortConfig.SortType = UnitSortTypes.Shipyard;
            else if (cpRB.Checked) sortConfig.SortType = UnitSortTypes.CP;
            else if (DurabilityRB.Checked) sortConfig.SortType = UnitSortTypes.Durability;
            else if (hpRB.Checked) sortConfig.SortType = UnitSortTypes.HP;
            else if (ShieldRB.Checked) sortConfig.SortType = UnitSortTypes.Shield;
            else if (RegenRB.Checked) sortConfig.SortType = UnitSortTypes.Regen;
            else if (STypeRB.Checked) sortConfig.SortType = UnitSortTypes.SType;
            else if (ATypeRB.Checked) sortConfig.SortType = UnitSortTypes.AType;
            else if (SpeedRB.Checked) sortConfig.SortType = UnitSortTypes.Speed;
            else if (AccelRB.Checked) sortConfig.SortType = UnitSortTypes.Accel;
            else if (TurnRB.Checked) sortConfig.SortType = UnitSortTypes.Turn;
            else if (dpsRawRB.Checked) sortConfig.SortType = UnitSortTypes.dpsRaw;
            else if (dpsAvgRB.Checked) sortConfig.SortType = UnitSortTypes.dpsAvg;
            else if (dpsArmorRB.Checked) sortConfig.SortType = UnitSortTypes.dpsArmor;
            else if (dpsShieldRB.Checked) sortConfig.SortType = UnitSortTypes.dpsShield;
            else if (ComplementRB.Checked) sortConfig.SortType = UnitSortTypes.Complement;
            else if (GarrisonSlotsRB.Checked) sortConfig.SortType = UnitSortTypes.GarrisonCap;
            else if (GarrisonValueRB.Checked) sortConfig.SortType = UnitSortTypes.GarrisonValue;
            else if (NameCountRB.Checked) sortConfig.SortType = UnitSortTypes.NameCount;
            else if (ChainStartRB.Checked) sortConfig.SortType = UnitSortTypes.ChainStart;
            else if (ChainEndRB.Checked) sortConfig.SortType = UnitSortTypes.ChainEnd;
            else if (pdRechargeRB.Checked) sortConfig.SortType = UnitSortTypes.pdRecharge;
            else if (pdRadiusRB.Checked) sortConfig.SortType = UnitSortTypes.pdRadius;
            else if (HealRB.Checked) sortConfig.SortType = UnitSortTypes.Heal;
            else if (DiscountPercentRB.Checked) sortConfig.SortType = UnitSortTypes.Discount;
            else if (TimeReductionRB.Checked) sortConfig.SortType = UnitSortTypes.TimeReduction;
            else if (IncomePercentRB.Checked) sortConfig.SortType = UnitSortTypes.incomePercent;
            else if (IncomeAmountRB.Checked) sortConfig.SortType = UnitSortTypes.incomeAmount;
            else if (CommandBonusRB.Checked) sortConfig.SortType = UnitSortTypes.CommandBonus;


            sortConfig.complementCP = ComplementCheckBox.Checked;
            if(sortConfig.SortType == UnitSortTypes.CP && sortConfig.complementCP) sortDocumentation += " (including complement)";
            sortConfig.DurabilityMode = DurabilityBox.SelectedIndex;
            if(sortConfig.SortType >= UnitSortTypes.Durability && sortConfig.SortType <= UnitSortTypes.Regen) sortDocumentation += " " + DurabilityBox.Text;
            sortConfig.fighterBomberMode = FighterTypeBox.SelectedIndex;
            sortConfig.upfrontReserveMode = ReserveBox.SelectedIndex;
            if (sortConfig.SortType >= UnitSortTypes.Complement) sortDocumentation += " " + FighterTypeBox.Text + "/" + ReserveBox.Text;
            sortConfig.Accuracy = AccuracyCheckBox.Checked;
            if (sortConfig.SortType >= UnitSortTypes.dpsAvg && sortConfig.SortType <= UnitSortTypes.dpsShield && sortConfig.Accuracy) sortDocumentation += " (including Accuracy)";
            sortConfig.HealMode = HealBox.SelectedIndex;
            if (sortConfig.SortType == UnitSortTypes.Heal) sortDocumentation += " " + HealBox.Text;
            sortConfig.CommandMode = CommandBox.SelectedIndex;
            sortConfig.CommandTypeMode = CommandTypeBox.SelectedIndex;
            if (sortConfig.SortType == UnitSortTypes.CommandBonus) sortDocumentation += " " + CommandBox.Text + " ("+CommandTypeBox.Text+")";

            //todo add modifiers to return string and vars
            if (AscComboBox.SelectedIndex > 0) sortConfig.Descending = true;
            else sortConfig.Descending = false;
            sortConfig.denomtype = DenomComboBox.Text;
            if (sortConfig.SortType > UnitSortTypes.Name) sortDocumentation += ", " + DenomComboBox.Text;
            sortDocumentation += ", " + AscComboBox.Text;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private UnitSortClass NewUserSort()
        {
            UnitSortClass corenne = new UnitSortClass();

            corenne.SortType = UnitSortTypes.Name;
            corenne.denomtype = "Absolute Value";

            return corenne;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            sortConfig = NewUserSort();
            loadFromConfig();
        }
    }
}
