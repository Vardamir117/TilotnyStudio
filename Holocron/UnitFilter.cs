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
    public partial class UnitFilter : Form
    {
        public UnitFilterClass filterConfig;
        public int UnitRBtype;
        public string filterDocumentation;
        public bool cancel;
        public List<faction> factions;
        public List<string> categories;
        public List<string> flags;
        public List<string> atypes;
        public List<string> stypes;

        private void hideavailpanels()
        {
            BuildPanel.Visible = false;
            PlanetPanel.Visible = false;
            InfluencePanel.Visible = false;
            LimitPanel.Visible = false;
        }

        public struct UnitFilterClass
        {
            public List<faction> factions; //need user facing and code name, the rest of the fields are quite excessive
            public List<string> categories;
            public List<string> flags;
            public List<string> atypes;
            public List<string> stypes;
            public bool invFaction;
            public bool invCats;
            public bool invFlags;
            public bool invAT;
            public bool invST;
            public int shipyardComparison;
            public int shipyardLevel;
            public int buildableMode;
            public int planetMode;
            public int influenceMode;
            public int limitMode;
            public int complementMode;
            public List<int> skirmishModes;
            public int pdMode;
            public int healMode;
            public int discountMode;
            public int incomeMode;
            public int commandMode;
        }

        public UnitFilter()
        {
            InitializeComponent();
        }

        public UnitFilterClass newFilter()
        {
            UnitFilterClass filter = new UnitFilterClass();
            filter.shipyardComparison = 0;
            filter.shipyardLevel = 0;
            filter.factions = new List<faction>();
            filter.categories = new List<string>();
            filter.flags = new List<string>();
            filter.atypes = new List<string>();
            filter.stypes = new List<string>();
            filter.skirmishModes = new List<int>();
            filter.skirmishModes.Add(0);

            filterDocumentation = "Any faction";
            return filter;
        }

        private void UnitFilter_Load(object sender, EventArgs e)
        {
            if (filterConfig.factions is null) filterConfig = newFilter();
            foreach (faction faction in factions) FactionListBox.Items.Add(faction);
            foreach (string category in categories) CategoryListBox.Items.Add(category);
            foreach (string flag in flags) FlagListBox.Items.Add(flag);
            foreach (string at in atypes) ArmorListBox.Items.Add(at);
            foreach (string st in stypes) ShieldListBox.Items.Add(st);
            loadFromConfig();
            switch (UnitRBtype)
            {
                case 0: //space unit
                    ShipyardLabel.Visible = true;
                    ShipyardBox.Visible = true;
                    ShipyardUpDown.Visible = true;
                    break;
                case 1: //ground companies
                    ComplementPanel.Visible = false; //Not just fighters, also buidling garrsions and hero escorts
                    break;
                case 2: //ground units
                    hideavailpanels();
                    ComplementPanel.Visible = false;
                    break;
                    //todo heroes, structures...
            }
        }

        private void loadFromConfig()
        {
            FactionCheckBox.Checked = filterConfig.invFaction;
            CategoryCheckBox.Checked = filterConfig.invCats;
            FlagCheckBox.Checked = filterConfig.invFlags;
            ArmorCheckBox.Checked = filterConfig.invAT;
            ShieldCheckBox.Checked = filterConfig.invST;

            if (filterConfig.buildableMode == 1) BuildTrueRB.Checked = true;
            else if (filterConfig.buildableMode == 2) BuildFalseRB.Checked = true;
            else BuildAnyRB.Checked = true;

            if (filterConfig.planetMode == 1) PlanetTrueRB.Checked = true;
            else if (filterConfig.planetMode == 2) PlanetFalseRB.Checked = true;
            else PlanetAnyRB.Checked = true;

            if (filterConfig.influenceMode == 1) InfTrueRB.Checked = true;
            else if (filterConfig.influenceMode == 2) InfFalseRB.Checked = true;
            else InfAnyRB.Checked = true;

            if (filterConfig.limitMode == 1) LimitTrueRB.Checked = true;
            else if (filterConfig.limitMode == 2) LimitFalseRB.Checked = true;
            else LimitAnyRB.Checked = true;

            if (filterConfig.complementMode == 1) ComplementTrueRB.Checked = true;
            else if (filterConfig.complementMode == 2) ComplementFalseRB.Checked = true;
            else ComplementAnyRB.Checked = true;

            SkirmishListBox.SelectedItems.Clear();
            foreach (int mode in filterConfig.skirmishModes) SkirmishListBox.SelectedIndices.Add(mode);

            if (filterConfig.pdMode == 1) PDTrueRB.Checked = true;
            else if (filterConfig.pdMode == 2) PDFalseRB.Checked = true;
            else PDAnyRB.Checked = true;

            if (filterConfig.healMode == 1) HealTrueRB.Checked = true;
            else if (filterConfig.healMode == 2) HealFalseRB.Checked = true;
            else HealAnyRB.Checked = true;

            if (filterConfig.discountMode == 1) DiscountTrueRB.Checked = true;
            else if (filterConfig.discountMode == 2) DiscountFalseRB.Checked = true;
            else DiscountAnyRB.Checked = true;

            if (filterConfig.incomeMode == 1) IncomeTrueRB.Checked = true;
            else if (filterConfig.incomeMode == 2) IncomeFalseRB.Checked = true;
            else IncomeAnyRB.Checked = true;

            if (filterConfig.commandMode == 1) CommandTrueRB.Checked = true;
            else if (filterConfig.commandMode == 2) CommandFalseRB.Checked = true;
            else CommandAnyRB.Checked = true;

            foreach (faction faction in filterConfig.factions) FactionListBox.SelectedItems.Add(faction);
            foreach (string category in filterConfig.categories) CategoryListBox.SelectedItems.Add(category);
            foreach (string flag in filterConfig.flags) FlagListBox.SelectedItems.Add(flag);
            foreach (string at in filterConfig.atypes) ArmorListBox.SelectedItems.Add(at);
            foreach (string st in filterConfig.stypes) ShieldListBox.SelectedItems.Add(st);

            ShipyardBox.SelectedIndex = filterConfig.shipyardComparison;
            ShipyardUpDown.Value = filterConfig.shipyardLevel;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            filterDocumentation = "Filter by: ";
            filterConfig.invFaction = FactionCheckBox.Checked;
            filterConfig.invCats = CategoryCheckBox.Checked;
            filterConfig.invFlags = FlagCheckBox.Checked;
            filterConfig.invAT = ArmorCheckBox.Checked;
            filterConfig.invST = ShieldCheckBox.Checked;

            filterConfig.factions.Clear();
            foreach (faction faction in FactionListBox.SelectedItems) filterConfig.factions.Add(faction);
            if(filterConfig.factions.Count > 0) filterDocumentation += "Faction";
            else filterDocumentation += "Any Faction";

            filterConfig.categories.Clear();
            foreach (string category in CategoryListBox.SelectedItems) filterConfig.categories.Add(category);
            if (filterConfig.categories.Count > 0) filterDocumentation += ", Category";

            filterConfig.flags.Clear();
            foreach (string flag in FlagListBox.SelectedItems) filterConfig.flags.Add(flag);
            if (filterConfig.flags.Count > 0) filterDocumentation += ", Property Flags";

            filterConfig.atypes.Clear();
            foreach (string armor in ArmorListBox.SelectedItems) filterConfig.atypes.Add(armor);
            if (filterConfig.atypes.Count > 0) filterDocumentation += ", Armor Type";

            filterConfig.stypes.Clear();
            foreach (string shield in ShieldListBox.SelectedItems) filterConfig.stypes.Add(shield);
            if (filterConfig.stypes.Count > 0) filterDocumentation += ", Shield Type";

            if (BuildTrueRB.Checked)
            {
                filterConfig.buildableMode = 1;
                if (BuildTrueRB.Visible) filterDocumentation += ", Buildable";
            }
            else if (BuildFalseRB.Checked)
            {
                filterConfig.buildableMode = 2;
                if (BuildTrueRB.Visible) filterDocumentation += ", not Buildable";
            }
            else filterConfig.buildableMode = 0;

            if (PlanetTrueRB.Checked)
            {
                filterConfig.planetMode = 1;
                if (PlanetTrueRB.Visible) filterDocumentation += ", Required Planets";
            }
            else if (PlanetFalseRB.Checked)
            {
                filterConfig.planetMode = 2;
                if (PlanetTrueRB.Visible) filterDocumentation += ", no Required Planets";
            }
            else filterConfig.planetMode = 0;

            if (InfTrueRB.Checked)
            {
                filterConfig.influenceMode = 1;
                if (InfTrueRB.Visible) filterDocumentation += ", Required Influence";
            }
            else if (InfFalseRB.Checked)
            {
                filterConfig.influenceMode = 2;
                if (InfTrueRB.Visible) filterDocumentation += ", no Required Influence";
            }
            else filterConfig.influenceMode = 0;

            if (LimitTrueRB.Checked)
            {
                filterConfig.limitMode = 1;
                if (LimitTrueRB.Visible) filterDocumentation += ", Build Limits";
            }
            else if (LimitFalseRB.Checked)
            {
                filterConfig.limitMode = 2;
                if (LimitTrueRB.Visible) filterDocumentation += ", no Build Limits";
            }
            else filterConfig.limitMode = 0;

            if (ComplementTrueRB.Checked)
            {
                filterConfig.complementMode = 1;
                if (ComplementTrueRB.Visible) filterDocumentation += ", has Complement";
            }
            else if (ComplementFalseRB.Checked)
            {
                filterConfig.complementMode = 2;
                if (ComplementTrueRB.Visible) filterDocumentation += ", has no Complement";
            }
            else filterConfig.complementMode = 0;

            filterConfig.skirmishModes.Clear();
            foreach (int mode in SkirmishListBox.SelectedIndices) filterConfig.skirmishModes.Add(mode);
            if(!(filterConfig.skirmishModes.Count == 1 && filterConfig.skirmishModes[0] == 0)) filterDocumentation += ", has no Complement";

            if (PDTrueRB.Checked)
            {
                filterConfig.pdMode = 1;
                if (PDTrueRB.Visible) filterDocumentation += ", has Point Defense";
            }
            else if (PDFalseRB.Checked)
            {
                filterConfig.pdMode = 2;
                if (PDTrueRB.Visible) filterDocumentation += ", no Point Defense";
            }
            else filterConfig.pdMode = 0;

            if (HealTrueRB.Checked)
            {
                filterConfig.healMode = 1;
                if (HealTrueRB.Visible) filterDocumentation += ", has Healing";
            }
            else if (HealFalseRB.Checked)
            {
                filterConfig.healMode = 2;
                if (HealTrueRB.Visible) filterDocumentation += ", no Healing";
            }
            else filterConfig.healMode = 0;

            if (DiscountTrueRB.Checked)
            {
                filterConfig.discountMode = 1;
                if (DiscountTrueRB.Visible) filterDocumentation += ", Discounts";
            }
            else if (DiscountFalseRB.Checked)
            {
                filterConfig.discountMode = 2;
                if (DiscountTrueRB.Visible) filterDocumentation += ", no Discounts";
            }
            else filterConfig.discountMode = 0;

            if (IncomeTrueRB.Checked)
            {
                filterConfig.incomeMode = 1;
                if (IncomeTrueRB.Visible) filterDocumentation += ", Income";
            }
            else if (IncomeFalseRB.Checked)
            {
                filterConfig.influenceMode = 2;
                if (IncomeTrueRB.Visible) filterDocumentation += ", no Income";
            }
            else filterConfig.incomeMode = 0;

            if (CommandTrueRB.Checked)
            {
                filterConfig.commandMode = 1;
                if (CommandTrueRB.Visible) filterDocumentation += ", Command Bonuses";
            }
            else if (CommandFalseRB.Checked)
            {
                filterConfig.commandMode = 2;
                if (CommandTrueRB.Visible) filterDocumentation += ", no Command Bonuses";
            }
            else filterConfig.commandMode = 0;

            filterConfig.shipyardComparison = ShipyardBox.SelectedIndex;
            filterConfig.shipyardLevel = (int)ShipyardUpDown.Value;
            if (ShipyardBox.Visible) filterDocumentation += ", Shipyard " + ShipyardBox.Text + " " + ShipyardUpDown.Value;

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            filterConfig = newFilter();
            loadFromConfig();
            FactionListBox.SelectedItems.Clear();
            CategoryListBox.SelectedItems.Clear();
            FlagListBox.SelectedItems.Clear();
            ArmorListBox.SelectedItems.Clear();
            ShieldListBox.SelectedItems.Clear();
        }
    }
}
