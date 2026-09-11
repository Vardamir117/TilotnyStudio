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
    public partial class TerrainLegend : Form
    {
        public TerrainLegend()
        {
            InitializeComponent();
        }

        private void TerrainCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TerrainLegend_Load(object sender, EventArgs e)
        {
            Bitmap Legend = new Bitmap(TerrainLegendPictureBox.Width, TerrainLegendPictureBox.Height);
            Graphics g = Graphics.FromImage(Legend);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, Legend.Width, Legend.Height);
            for(int i = 0; i < 8; i++)
            {
                SolidBrush brush = new SolidBrush(getTerrainColor(i));
                g.FillEllipse(brush, 10, i * 30 + 10, 21, 21);

                g.DrawString(getTerrainName(i), new Font(this.Font.Name, 12, this.Font.Style), brush, 40, i * 30 + 13);
                TerrainLegendPictureBox.Image = Legend;
            }
        }
    }
}
