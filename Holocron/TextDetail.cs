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
    public partial class TextDetail : Form
    {
        public TextDetail()
        {
            InitializeComponent();
        }

        public string detail;

        private void TextDetail_Load(object sender, EventArgs e)
        {
            DetailTextBox.Text = detail;
        }

        private void DetailCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
