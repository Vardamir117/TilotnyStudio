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
    public partial class PlanetSort : Form
    {
        public PlanetSortClass sortConfig;
        public string sortDocumentation;
        public bool cancel;

        public PlanetSort()
        {
            InitializeComponent();
        }

        private void PlanetSort_Load(object sender, EventArgs e)
        {
            loadFromConfig();
        }

        public enum PlanetSortTypes
        {
            Internal,
            Terrain,
            GroundMap,
            SpaceMap,
            Name, //Put all string sort classes before Name so < can find them
            Income,
            shipyard,
            Starbase,
            Slots,
            Pop,
            Potential,
            Production,
            Usage,
            X,
            Y,
            R,
        }

        public struct PlanetSortClass
        {
            public PlanetSortTypes SortType;
            public int productionMode;
            public int usageMode;
            public bool Descending;
        }

        private void loadFromConfig()
        {
            switch (sortConfig.SortType)
            {
                case PlanetSortTypes.Name:
                    NameRB.Checked = true;
                    break;
                case PlanetSortTypes.Income:
                    IncomeRB.Checked = true;
                    break;
                case PlanetSortTypes.shipyard:
                    ShipyardRB.Checked = true;
                    break;
                case PlanetSortTypes.Starbase:
                    StarbaseRB.Checked = true;
                    break;
                case PlanetSortTypes.Slots:
                    SlotsRB.Checked = true;
                    break;
                case PlanetSortTypes.Pop:
                    PopRB.Checked = true;
                    break;
                case PlanetSortTypes.Potential:
                    PotentialRB.Checked = true;
                    break;
                case PlanetSortTypes.Terrain:
                    TerrainRB.Checked = true;
                    break;
                case PlanetSortTypes.Production:
                    ProductionRB.Checked = true;
                    break;
                case PlanetSortTypes.Usage:
                    UsageRB.Checked = true;
                    break;
                case PlanetSortTypes.SpaceMap:
                    SpaceRB.Checked = true;
                    break;
                case PlanetSortTypes.GroundMap:
                    GroundRB.Checked = true;
                    break;
                case PlanetSortTypes.Internal:
                    InternalRB.Checked = true;
                    break;
                case PlanetSortTypes.X:
                    XRB.Checked = true;
                    break;
                case PlanetSortTypes.Y:
                    YRB.Checked = true;
                    break;
                case PlanetSortTypes.R:
                    RRB.Checked = true;
                    break;
            }

            if (sortConfig.Descending) AscComboBox.SelectedIndex = 1;
            else AscComboBox.SelectedIndex = 0;

            ProductionBox.SelectedIndex = sortConfig.productionMode;
            UsageBox.SelectedIndex = sortConfig.usageMode;
        }

        private void PlanetSortAcceptButton_Click(object sender, EventArgs e)
        {
            foreach (Control rb in this.Controls)
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
            if (NameRB.Checked) sortConfig.SortType = PlanetSortTypes.Name;
            else if (IncomeRB.Checked) sortConfig.SortType = PlanetSortTypes.Income;
            else if (ShipyardRB.Checked) sortConfig.SortType = PlanetSortTypes.shipyard;
            else if (StarbaseRB.Checked) sortConfig.SortType = PlanetSortTypes.Starbase;
            else if (SlotsRB.Checked) sortConfig.SortType = PlanetSortTypes.Slots;
            else if (PopRB.Checked) sortConfig.SortType = PlanetSortTypes.Pop;
            else if (PotentialRB.Checked) sortConfig.SortType = PlanetSortTypes.Potential;
            else if (TerrainRB.Checked) sortConfig.SortType = PlanetSortTypes.Terrain;
            else if (ProductionRB.Checked) sortConfig.SortType = PlanetSortTypes.Production;
            else if (UsageRB.Checked) sortConfig.SortType = PlanetSortTypes.Usage;
            else if (SpaceRB.Checked) sortConfig.SortType = PlanetSortTypes.SpaceMap;
            else if (GroundRB.Checked) sortConfig.SortType = PlanetSortTypes.GroundMap;
            else if (InternalRB.Checked) sortConfig.SortType = PlanetSortTypes.Internal;
            else if (XRB.Checked) sortConfig.SortType = PlanetSortTypes.X;
            else if (YRB.Checked) sortConfig.SortType = PlanetSortTypes.Y;
            else if (RRB.Checked) sortConfig.SortType = PlanetSortTypes.R;

            sortConfig.productionMode = ProductionBox.SelectedIndex;
            if(ProductionRB.Checked) sortDocumentation += " " + ProductionBox.Text;

            sortConfig.usageMode = UsageBox.SelectedIndex;
            if (UsageRB.Checked) sortDocumentation += ", " + UsageBox.Text;

            if (AscComboBox.SelectedIndex > 0) sortConfig.Descending = true;
            else sortConfig.Descending = false;
            sortDocumentation += ", " + AscComboBox.Text;

            this.Close();
        }

        private void PlanetSortCancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private PlanetSortClass NewPlanetSort()
        {
            PlanetSortClass corenne = new PlanetSortClass();

            corenne.SortType = PlanetSortTypes.Name;
            sortConfig.productionMode = 0;
            sortConfig.usageMode = 0;

            return corenne;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            sortConfig = NewPlanetSort();
            loadFromConfig();
        }
    }
}
