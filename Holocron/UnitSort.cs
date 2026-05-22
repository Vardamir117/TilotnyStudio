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
            GarrisonSlotsRB.Location = ComplementRB.Location;

            switch (UnitRBtype)
            {
                case 0: //space unit
                    denomTypes.Add("Per Crew");
                    ShipyardRB.Visible = true;
                    CrewRB.Visible = true;
                    ComplementRB.Visible = true;
                    FighterTypeBox.Visible = true;
                    ReserveBox.Visible = true;
                    GarrisonSlotsRB.Visible = false;
                    break;
                case 1: //ground companies
                    denomTypes.Add("Per Unit in Company");
                    break;
                case 2: //ground units
                    new List<string> { "Absolute Value", "Per Combat Power" };
                    PriceRB.Visible = false;
                    SkPriceRB.Visible = false;
                    BuildTimeRB.Visible = false;
                    SkBuildTimeRB.Visible = false;
                    break;
                //todo heroes, structures...
            }

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
                default:
                    break;
            }

            //todo = Set RB and such from passed UnitConfig
            DurabilityBox.SelectedIndex = sortConfig.DurabilityMode;
            FighterTypeBox.SelectedIndex = sortConfig.fighterBomberMode;
            ReserveBox.SelectedIndex = sortConfig.fighterBomberMode;
            DenomComboBox.Text = sortConfig.denomtype;
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

            sortConfig.complementCP = ComplementCheckBox.Checked;
            if(sortConfig.SortType >= UnitSortTypes.CP && sortConfig.complementCP) sortDocumentation += " (including complement)";
            sortConfig.DurabilityMode = DurabilityBox.SelectedIndex;
            if(sortConfig.SortType >= UnitSortTypes.Durability && sortConfig.SortType <= UnitSortTypes.Regen) sortDocumentation += " " + DurabilityBox.Text;
            sortConfig.fighterBomberMode = FighterTypeBox.SelectedIndex;
            sortConfig.fighterBomberMode = ReserveBox.SelectedIndex;
            if (sortConfig.SortType >= UnitSortTypes.Complement) sortDocumentation += " " + FighterTypeBox.Text + "/" + ReserveBox.Text;
            sortConfig.Accuracy = AccuracyCheckBox.Checked;
            if (sortConfig.SortType >= UnitSortTypes.dpsAvg && sortConfig.SortType <= UnitSortTypes.dpsShield) sortDocumentation += " (including Accuracy)";

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
