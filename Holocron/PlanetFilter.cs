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
    public partial class PlanetFilter : Form
    {
        public PlanetFilterClass filterConfig;
        public string filterDocumentation;
        public List<galacticConquest> GCFull;
        public bool cancel;

        public PlanetFilter()
        {
            InitializeComponent();
        }

        public struct PlanetFilterClass
        {
            public List<galacticConquest> GCs;
            public List<int> terrains;
            public bool UnionIntersection;
            public bool Progressive;
            public bool Regional;
            public bool Historical;
            public bool Infinity;
            public unitFilter unitFilter;
            public buildingFilter buildingFilter;
            public int shipyardComparison;
            public int shipyardLevel;
            public int starbaseComparison;
            public int starbaseLevel;
            public int slotsComparison;
            public int slotsLevel;
            public int incomeComparison;
            public int incomeLevel;
            public int potentialComparison;
            public int potentialLevel;
            public int hubMode;
            public int spawnMode;
            public int regionalMode;
        }

        public enum unitFilter
        {
            any,
            has,
            space,
            ground,
            none,
        }

        public enum buildingFilter
        {
            any,
            has,
            income,
            discount,
            money,
            nonfinancial,
            none,
        }

        public PlanetFilterClass newFilter()
        {
            PlanetFilterClass filter = new PlanetFilterClass();
            filter.GCs = new List<galacticConquest>();
            filter.terrains = new List<int>();
            filter.UnionIntersection = true;
            filter.Progressive = true;
            filter.Regional = true;
            filter.Historical = true;
            filter.Infinity = true;
            filter.unitFilter = unitFilter.any;
            filter.buildingFilter = buildingFilter.any;
            filter.shipyardComparison = 0;
            filter.shipyardLevel = 0; //Should never be below 1 in EaWX, but keep extensible
            filter.starbaseComparison = 0;
            filter.starbaseLevel = 1;
            filter.slotsComparison = 0;
            filter.slotsLevel = 0;
            filter.incomeComparison = 0;
            filter.incomeLevel = 0;
            filter.potentialComparison = 0;
            filter.potentialLevel = 0;
            filter.hubMode = 0;
            filter.spawnMode = 0;

            filterDocumentation = "Any faction";
            return filter;
        }

        private void populateFilterGCListBox()
        {
            FilterGCListBox.Items.Clear();
            foreach (galacticConquest GC in GCFull)
            {
                switch (GC.Type)
                {
                    case GCType.Progressive:
                        if (FilterProgressiveCheckBox.Checked) FilterGCListBox.Items.Add(GC);
                        break;
                    case GCType.Regional:
                        if (FilterRegionalCheckBox.Checked) FilterGCListBox.Items.Add(GC);
                        break;
                    case GCType.Historical:
                        if (FilterHistoricalCheckBox.Checked) FilterGCListBox.Items.Add(GC);
                        break;
                    case GCType.Infinity:
                    case GCType.InfinityLayoutCopy:
                        if (FilterInfinityCheckBox.Checked) FilterGCListBox.Items.Add(GC);
                        break;
                }
            }
        }

        private void FilterProgressiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateFilterGCListBox();
        }

        private void FilterRegionalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateFilterGCListBox();
        }

        private void FilterHistoricalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateFilterGCListBox();
        }

        private void FilterInfinityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            populateFilterGCListBox();
        }

        private void FilterGCSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < FilterGCListBox.Items.Count; i++) FilterGCListBox.SetSelected(i, true);
        }

        private void FilterGCClear_Click(object sender, EventArgs e)
        {
            FilterGCListBox.SelectedItems.Clear();
        }

        private void PlanetFilter_Load(object sender, EventArgs e)
        {
            if (filterConfig.GCs is null) filterConfig = newFilter();
            loadFromConfig();
        }

        private void loadFromConfig()
        {
            FilterProgressiveCheckBox.Checked = filterConfig.Progressive;
            FilterRegionalCheckBox.Checked = filterConfig.Regional;
            FilterHistoricalCheckBox.Checked = filterConfig.Historical;
            FilterInfinityCheckBox.Checked = filterConfig.Infinity;
            if(filterConfig.UnionIntersection) UnionRB.Checked = true;
            else IntersectionRB.Checked = true;
            populateFilterGCListBox();

            foreach (galacticConquest GC in filterConfig.GCs)
            {
                FilterGCListBox.SelectedItems.Add(GC);
            }

            TerrainListBox.SelectedItems.Clear();
            foreach (int terrain in filterConfig.terrains)
            {
                TerrainListBox.SelectedIndices.Add(terrain);
            }

            if (filterConfig.hubMode == 1) HubTrueRB.Checked = true;
            else if (filterConfig.hubMode == 2) HubFalseRB.Checked = true;
            else HubAnyRB.Checked = true;

            if (filterConfig.spawnMode == 1) SpawnTrueRB.Checked = true;
            else if (filterConfig.spawnMode == 2) SpawnFalseRB.Checked = true;
            else SpawnAnyRB.Checked = true;

            if (filterConfig.regionalMode == 1) RegionalTrueRB.Checked = true;
            else if (filterConfig.regionalMode == 2) RegionalFalseRB.Checked = true;
            else RegionalAnyRB.Checked = true;

            ShipyardBox.SelectedIndex = filterConfig.shipyardComparison;
            ShipyardUpDown.Value = filterConfig.shipyardLevel;

            StarbaseBox.SelectedIndex = filterConfig.starbaseComparison;
            StarbaseUpDown.Value = filterConfig.starbaseLevel;

            SlotsBox.SelectedIndex = filterConfig.slotsComparison;
            SlotsUpDown.Value = filterConfig.slotsLevel;

            IncomeBox.SelectedIndex = filterConfig.incomeComparison;
            IncomeUpDown.Value = filterConfig.incomeLevel;

            PotentialBox.SelectedIndex = filterConfig.potentialComparison;
            PotentialUpDown.Value = filterConfig.potentialLevel;

            switch (filterConfig.unitFilter)
            {
                default://case unitFilter.any:
                    UnitsAnyRB.Checked = true;
                    break;
                case unitFilter.has:
                    UnitsTrueRB.Checked = true;
                    break;
                case unitFilter.space:
                    UnitsSpaceRB.Checked = true;
                    break;
                case unitFilter.ground:
                    UnitsGroundRB.Checked = true;
                    break;
                case unitFilter.none:
                    UnitsFalseRB.Checked = true;
                    break;
            }

            switch (filterConfig.buildingFilter)
            {
                default://case buildingFilter.any:
                    BuildingsAnyRB.Checked = true;
                    break;
                case buildingFilter.has:
                    BuildingsTrueRB.Checked = true;
                    break;
                case buildingFilter.income:
                    BuildingsIncomeRB.Checked = true;
                    break;
                case buildingFilter.discount:
                    BuildingsDiscountRB.Checked = true;
                    break;
                case buildingFilter.money:
                    BuildingsMoneyRB.Checked = true;
                    break;
                case buildingFilter.nonfinancial:
                    BuildingsPoorRB.Checked = true;
                    break;
                case buildingFilter.none:
                    BuildingsFalseRB.Checked = true;
                    break;
            }
        }

        private void UnitFilterAcceptButton_Click(object sender, EventArgs e)
        {
            filterDocumentation = "Filter by: ";
            if(UnionRB.Checked) filterConfig.UnionIntersection = true;
            else filterConfig.UnionIntersection = false;
            filterConfig.Progressive = FilterProgressiveCheckBox.Checked;
            filterConfig.Regional = FilterRegionalCheckBox.Checked;
            filterConfig.Historical = FilterHistoricalCheckBox.Checked;
            filterConfig.Infinity = FilterInfinityCheckBox.Checked;

            filterConfig.GCs.Clear();
            foreach (galacticConquest GC in FilterGCListBox.SelectedItems)
            {
                filterConfig.GCs.Add(GC);
            }
            if(filterConfig.GCs.Count > 0) filterDocumentation += "Chosen Galactic Conquests";
            else filterDocumentation += "Any Galactic Conquest";

            filterConfig.terrains.Clear();
            foreach (int terrain in TerrainListBox.SelectedIndices)
            {
                filterConfig.terrains.Add(terrain);
            }
            if (filterConfig.terrains.Count > 0) filterDocumentation += ", Terrain";

            if (HubTrueRB.Checked)
            {
                filterConfig.hubMode = 1;
                if (HubTrueRB.Visible) filterDocumentation += ", Trade Hubs";
            }
            else if (HubFalseRB.Checked)
            {
                filterConfig.hubMode = 2;
                if (HubFalseRB.Visible) filterDocumentation += ", no Trade Hubs";
            }
            else filterConfig.hubMode = 0;

            if (SpawnTrueRB.Checked)
            {
                filterConfig.spawnMode = 1;
                if (SpawnTrueRB.Visible) filterDocumentation += ", Spawn Sets";
            }
            else if (SpawnFalseRB.Checked)
            {
                filterConfig.spawnMode = 2;
                if (SpawnFalseRB.Visible) filterDocumentation += ", no Spawn Sets";
            }
            else filterConfig.spawnMode = 0;

            if (RegionalTrueRB.Checked)
            {
                filterConfig.regionalMode = 1;
                if (RegionalTrueRB.Visible) filterDocumentation += ", Regional fighters";
            }
            else if (RegionalFalseRB.Checked)
            {
                filterConfig.regionalMode = 2;
                if (RegionalFalseRB.Visible) filterDocumentation += ", no Regional fighters";
            }
            else filterConfig.regionalMode = 0;

            //todo no text for default settings
            filterConfig.shipyardComparison = ShipyardBox.SelectedIndex;
            filterConfig.shipyardLevel = (int)ShipyardUpDown.Value;
            if (ShipyardBox.Visible && !(filterConfig.shipyardLevel == 0 && filterConfig.shipyardComparison == 0)) filterDocumentation += ", Shipyard " + ShipyardBox.Text + " " + ShipyardUpDown.Value;

            filterConfig.starbaseComparison = StarbaseBox.SelectedIndex;
            filterConfig.starbaseLevel = (int)StarbaseUpDown.Value;
            if (StarbaseBox.Visible && !(filterConfig.starbaseLevel == 1 && filterConfig.starbaseComparison == 0)) filterDocumentation += ", Starbase " + StarbaseBox.Text + " " + StarbaseUpDown.Value;

            filterConfig.slotsComparison = SlotsBox.SelectedIndex;
            filterConfig.slotsLevel = (int)SlotsUpDown.Value;
            if (SlotsBox.Visible && !(filterConfig.slotsLevel == 0 && filterConfig.slotsComparison == 0)) filterDocumentation += ", Ground Slots " + SlotsBox.Text + " " + SlotsUpDown.Value;

            filterConfig.incomeComparison = IncomeBox.SelectedIndex;
            filterConfig.incomeLevel = (int)IncomeUpDown.Value;
            if (IncomeBox.Visible && !(filterConfig.incomeLevel == 0 && filterConfig.incomeComparison == 0)) filterDocumentation += ", Income " + IncomeBox.Text + " " + IncomeUpDown.Value;

            filterConfig.potentialComparison = PotentialBox.SelectedIndex;
            filterConfig.potentialLevel = (int)PotentialUpDown.Value;
            if (PotentialBox.Visible && !(filterConfig.potentialLevel == 0 && filterConfig.potentialComparison == 0)) filterDocumentation += ", Potential " + PotentialBox.Text + " " + PotentialUpDown.Value;

            if (UnitsTrueRB.Checked)
            {
                filterConfig.unitFilter = unitFilter.has;
                filterDocumentation += ", Planet units";
            }
            else if (UnitsSpaceRB.Checked)
            {
                filterConfig.unitFilter = unitFilter.space;
                filterDocumentation += ", Planet space units";
            }
            else if (UnitsGroundRB.Checked)
            {
                filterConfig.unitFilter = unitFilter.ground;
                filterDocumentation += ", Planet ground units";
            }
            else if (UnitsFalseRB.Checked)
            {
                filterConfig.unitFilter = unitFilter.none;
                filterDocumentation += ", no Planet units";
            }
            else filterConfig.unitFilter = unitFilter.any;

            if (BuildingsTrueRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.has;
                filterDocumentation += ", Planet structutes";
            }
            else if (BuildingsIncomeRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.income;
                filterDocumentation += ", Income structures";
            }
            else if (BuildingsDiscountRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.discount;
                filterDocumentation += ", Discount structures";
            }
            else if (BuildingsMoneyRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.money;
                filterDocumentation += ", Income or Discount Structures";
            }
            else if (BuildingsPoorRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.nonfinancial;
                filterDocumentation += ", Nonfinancial structures";
            }
            else if (BuildingsFalseRB.Checked)
            {
                filterConfig.buildingFilter = buildingFilter.none;
                filterDocumentation += ", No Planet structutes";
            }
            else filterConfig.buildingFilter = buildingFilter.any;

            this.Close();
        }

        private void UnitFilterCancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            filterConfig = newFilter();
            loadFromConfig();
        }
    }
}
