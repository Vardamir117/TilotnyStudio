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
    public partial class IconPickAndAdd : Form
    {
        public bool cancel = false;
        public bool addedicon = false;
        public string icon;
        public entities entities;

        public IconPickAndAdd()
        {
            InitializeComponent();
        }

        private void IconPickAndAdd_Load(object sender, EventArgs e)
        {
            populateIconList();
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {

        }

        private void populateIconList()
        {
            IconListBox.Items.Clear();
            foreach (IconData icon in entities.IconData)
            {
                if (IconSearchTextBox.Text == "" || icon.id.Contains(IconSearchTextBox.Text)) IconListBox.Items.Add(IconSearchTextBox.Text);
            }
        }

        private void IconSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            populateIconList();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
        }

        private void IconListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            IconData icondata = DatParser.GetIconData((string)IconListBox.SelectedItem, entities);
            if (icondata.size_x > 0 && entities.MTmaster != null)
            {
                // Create a Graphics object to do the drawing, *with the new bitmap as the target*
                using (Graphics g = Graphics.FromImage(IconPictureBox.Image))
                {
                    g.DrawImage(entities.MTmaster, 0, 0, new Rectangle(icondata.origin_x, icondata.origin_y, icondata.size_x, icondata.size_y), GraphicsUnit.Pixel);
                }
            }
        }
    }
}
