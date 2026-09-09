using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SharedFunctions;

namespace Holocron
{
    public partial class FactionLegend : Form
    {
        public List<faction> Factions;

        public FactionLegend()
        {
            InitializeComponent();
        }

        private void TerrainCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DrawLegend()
        {
            if(Factions is null)
            {
                //I do not understand why this matters. Somehow the Korean localization or a specific framework version is... calling this twice but somehow skipping lines? //MessageBox.Show("Null faction list");
                return;
            }
            Bitmap Legend = new Bitmap(FactionLegendPictureBox.Width, FactionLegendPictureBox.Height);
            Graphics g = Graphics.FromImage(Legend);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, Legend.Width, Legend.Height);
            for (int i = 0; i < Factions.Count; i++)
            {
                faction faction = Factions[i];
                SolidBrush brush = new SolidBrush(Color.FromArgb(255, faction.color[0], faction.color[1], faction.color[2]));
                g.FillEllipse(brush, 10, i * 30 + 10, 21, 21);

                g.DrawString(faction.textname, new Font(this.Font.Name, 12, this.Font.Style), brush, 40, i * 30 + 13);
                FactionLegendPictureBox.Image = Legend;
            }
        }

        private void FactionLegend_Load(object sender, EventArgs e)
        {
            DrawLegend();
        }

        private void FactionLegendPictureBox_Resize(object sender, EventArgs e)
        {
            DrawLegend();
        }
    }
}
