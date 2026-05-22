using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGASharpLib
{
    public partial class Form1 : Form
    {
        TGA T;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Byte[] mtdbytes = File.ReadAllBytes("MT_CommandBar.tga");

            Bitmap bitmap = (Bitmap)Image.FromFile("MT_CommandBar.tga");
            pictureBox1.Image = bitmap;
            ShowTga();
            //read_MTD(mtdbytes);
        }

        void ShowTga()
        {
            T = new TGA("MT_CommandBar.tga");
            Bitmap BMP = (Bitmap)T;
            Bitmap Thumb = T.GetPostageStampImage();
        }

        void read_MTD(Byte[] mtdbytes)
        {//https://paulbourke.net/dataformats/tga/   https://netghost.narod.ru/gff/vendspec/tga/tga.txt https://github.com/ALEXGREENALEX/TGASharpLib/blob/master/tga_specs.pdf
            bool visible = true;
        }
    }
}
