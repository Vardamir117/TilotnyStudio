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
    public partial class Loading : Form
    {
        public Loading()
        {
            InitializeComponent();
        }

        public void CloseLoadScreen()
        {
            this.BeginInvoke(new Action(() => this.Close()));
        }

        public void ChangeText(string text)
        {
            label1.BeginInvoke(new Action(() => label1.Text = text));
            label1.BeginInvoke(new Action(() => label1.Update()));
        }

        public void SetQuote(string text)
        {
            QuoteLabel.BeginInvoke(new Action(() => QuoteLabel.Text = text));
            QuoteLabel.BeginInvoke(new Action(() => QuoteLabel.Update()));
        }

        private void Loading_Load(object sender, EventArgs e)
        {
            label2.Update();
            timer1.Start();
        }

        public int holoindex = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (holoindex)
            {
                case 0:
                    pictureBox1.Image = Properties.Resources._0;
                    break;
                case 1:
                    pictureBox1.Image = Properties.Resources._1;
                    break;
                case 2:
                    pictureBox1.Image = Properties.Resources._2;
                    break;
                case 3:
                    pictureBox1.Image = Properties.Resources._3;
                    break;
                case 4:
                    pictureBox1.Image = Properties.Resources._4;
                    break;
                case 5:
                    pictureBox1.Image = Properties.Resources._5;
                    break;
                case 6:
                    pictureBox1.Image = Properties.Resources._6;
                    break;
                case 7:
                    pictureBox1.Image = Properties.Resources._7;
                    holoindex = -1;
                    break;
            }
            pictureBox1.Update();
            //label1.Update();
            holoindex++;
        }
    }
}
