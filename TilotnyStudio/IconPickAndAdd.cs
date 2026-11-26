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
        public string localmod;
        public entities entities;
        public bool[,] MTDArray;
        public string mainicon;

        public IconPickAndAdd()
        {
            InitializeComponent();
        }

        private void IconPickAndAdd_Load(object sender, EventArgs e)
        {
            populateIconList();
            if(!(icon is null)) IconListBox.SelectedItem = icon.ToUpper();
            ShowBooleanMap();
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            if (icon == "") MessageBox.Show("An icon must be selected");
            else this.Close();
        }

        private void populateIconList()
        {
            IconListBox.Items.Clear();
            string search = IconSearchTextBox.Text.ToUpper();
            foreach (IconData icon in entities.IconData)
            {
                if (search == "" || icon.id.Contains(search)) IconListBox.Items.Add(icon.id);
            }
        }

        private void IconSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            populateIconList();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            this.Close();
        }

        private void ShowBooleanMap()
        {
            UsedMapPictureBox.Image = new Bitmap(UsedMapPictureBox.Width, UsedMapPictureBox.Height);

            Graphics g = Graphics.FromImage(UsedMapPictureBox.Image);

            const int interval = 16;

            for (int x = 0; x < MTDArray.GetLength(0) + interval; x += interval)
            {
                for (int y = 0; y < MTDArray.GetLength(1) + interval; y += interval)
                {
                    int trues = 0;
                    int falses = 0;
                    for (int ix = x; ix < x + interval && ix < MTDArray.GetLength(0); ix++)
                    {
                        for (int iy = y; iy < y + interval && iy < MTDArray.GetLength(1); iy++)
                        {
                            if (MTDArray[ix, iy]) trues++;
                            else falses++;
                        }
                    }
                    Color color = Color.Green;
                    if (trues >= falses) color = Color.Red;
                    g.FillRectangle(new SolidBrush(color), x / interval, y / interval, 1, 1);
                }
            }    
        }

        private void IconListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = (string)IconListBox.SelectedItem;
            IconPictureBox.Image = new Bitmap(IconPictureBox.Width, IconPictureBox.Height);
            IconData icondata = DatParser.GetIconData(selected, entities);
            if (icondata.size_x > 0 && entities.MTmaster != null)
            {
                // Create a Graphics object to do the drawing, *with the new bitmap as the target*
                using (Graphics g = Graphics.FromImage(IconPictureBox.Image))
                {
                    g.DrawImage(entities.MTmaster, 0, 0, new Rectangle(icondata.origin_x, icondata.origin_y, icondata.size_x, icondata.size_y), GraphicsUnit.Pixel);
                }
            }
            SelectedIconLabel.Text = "Selected: " + selected;
            icon = selected;
        }

        private void AddIconButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Filter = "TGA files (*.tga)|*.tga|All Files|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Image image = new Bitmap(1, 1);
            try
            {
                image = (Bitmap)Image.FromFile(openFileDialog1.FileName);
            }
            catch
            {
                try
                {
                    image = (Bitmap)(new TGA(openFileDialog1.FileName));
                }
                catch
                {
                    MessageBox.Show("Unable to parse source file as an image");
                    return;
                }
            };

            string iconname = SharedFunctions.LastFolderOrFile(openFileDialog1.FileName);
            bool[,] corenne = SharedFunctions.saveIcon(iconname, image, MTDArray, entities, localmod);
            if (corenne.Length < 1) return;
            MTDArray = corenne;

            ShowBooleanMap();

            populateIconList();

            IconListBox.SelectedItem = iconname;
            addedicon = true;
        }

        //todo move this all to a funtion that takes a list of filenames as args
        //detecting space fails badly 
    }
}
