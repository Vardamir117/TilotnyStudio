using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SharedFunctions;

namespace TilotnyStudio
{
    public partial class SubmodSetup : Form
    {
        public string localmodpath;
        public string steammodpath;

        public List<String> Args = new List<string>();
        public bool reload = false;
        string displayargs = "";

        public SubmodSetup()
        {
            InitializeComponent();
        }

        private void SubmodSetup_Load(object sender, EventArgs e)
        {
            ShortcutLabel.Text = "";
            foreach (string folder in Directory.GetDirectories(localmodpath))
            {
                LocalListbox.Items.Add(LastFolderOrFile(folder));
            }

            foreach (string folder in Directory.GetDirectories(steammodpath))
            {
                WorkshopListbox.Items.Add(LastFolderOrFile(folder));
            }
        }

        private void SubmodButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog openFileDialog1 = new SaveFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Filter = "Shortcuts (*.lnk)|*.lnk";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string param = "\"";
            for(int i=0; i< Args.Count; i++)
            {
                if (i > 0) param += ";";
                param += Args[i];
            }
            param += "\"";

            WshShell wsh = new WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(
                openFileDialog1.FileName)
                as IWshRuntimeLibrary.IWshShortcut;
            shortcut.Arguments = param;
            shortcut.TargetPath = Application.ExecutablePath;
            // not sure about what this is for
            shortcut.WindowStyle = 1;
            shortcut.Description = "Launch EaWX Holocron";
            shortcut.WorkingDirectory = UpOneFolder(AppContext.BaseDirectory);
            shortcut.IconLocation = Application.ExecutablePath;
            shortcut.Save();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LocalButton_Click(object sender, EventArgs e)
        {
            if(LocalListbox.SelectedItems.Count > 0)
            {
                Args.Add(Path.Combine(localmodpath, LocalListbox.Text,"Data"));
                displayargs += LocalListbox.Text;

                ShortcutLabel.Text = "Mod stack: " + displayargs;
            }
        }

        private void WorkshopButton_Click(object sender, EventArgs e)
        {
            if (WorkshopListbox.SelectedItems.Count > 0)
            {
                Args.Add(Path.Combine(steammodpath, WorkshopListbox.Text,"Data"));
                displayargs += WorkshopListbox.Text;

                ShortcutLabel.Text = "Mod stack: " + displayargs;
            } 
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            Args.Clear();
            displayargs = "";
            ShortcutLabel.Text = "";
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            if(Args.Count > 0) reload = true;
            this.Close();
        }
    }
}
