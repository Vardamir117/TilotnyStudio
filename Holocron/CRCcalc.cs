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
    public partial class CRCcalc : Form
    {
        public CRCcalc()
        {
            InitializeComponent();
        }

        private void CRCclose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CRCInTextBox_TextChanged(object sender, EventArgs e)
        {
            CRCOutBox.Value = Text_Entry.calculateCRC(CRCInTextBox.Text.ToUpper());
        }
    }
}
