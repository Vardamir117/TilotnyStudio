using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TilotnyStudio
{
    public partial class TierPick : Form
    {
        public int tier = 1;
        public bool cancel = false;

        public TierPick()
        {
            InitializeComponent();
        }

        private void TierCancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private void TierAcceptButton_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked) tier = 1;
            else if (radioButton2.Checked) tier = 2;
            else if (radioButton3.Checked) tier = 3;
            else if (radioButton4.Checked) tier = 4;
            else if (radioButton5.Checked) tier = 5;
            else if (radioButton6.Checked) tier = 6;
            else if (radioButton7.Checked) tier = 7;
            else if (radioButton8.Checked) tier = 8;
            else if (radioButton9.Checked) tier = 9;

            int modifier = 0;
            if (EduCheckBox.Checked) modifier++;
            if (VicCheckBox.Checked) modifier++;
            if (BrilCheckBox.Checked) modifier++;
            if (PolCheckBox.Checked) modifier--;
            if (DefCheckBox.Checked) modifier--;
            if (IncCheckBox.Checked) modifier--;
            if (modifier > 2) modifier = 2;
            if (modifier < -2) modifier = -2;
            tier += modifier;
            this.Close();
        }

        private void TierPick_Load(object sender, EventArgs e)
        {
            if (tier == 1) radioButton1.Checked = true;
            else if (tier == 2) radioButton2.Checked = true;
            else if (tier == 3) radioButton3.Checked = true;
            else if (tier == 4) radioButton4.Checked = true;
            else if (tier == 5) radioButton5.Checked = true;
            else if (tier == 6) radioButton6.Checked = true;
            else if (tier == 7) radioButton7.Checked = true;
            else if (tier == 8) radioButton8.Checked = true;
            else if (tier >= 9) radioButton9.Checked = true;
            else radioButton0.Checked = true;
        }
    }
}
