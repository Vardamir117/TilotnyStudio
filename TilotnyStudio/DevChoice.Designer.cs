namespace TilotnyStudio
{
    partial class DevChoice
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DevChoice));
            this.PlanetsCheckBox = new System.Windows.Forms.CheckBox();
            this.TRbutton = new System.Windows.Forms.Button();
            this.FotRbutton = new System.Windows.Forms.Button();
            this.Revbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PlanetsCheckBox
            // 
            this.PlanetsCheckBox.AutoSize = true;
            this.PlanetsCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PlanetsCheckBox.Location = new System.Drawing.Point(76, 129);
            this.PlanetsCheckBox.Name = "PlanetsCheckBox";
            this.PlanetsCheckBox.Size = new System.Drawing.Size(110, 24);
            this.PlanetsCheckBox.TabIndex = 3;
            this.PlanetsCheckBox.Text = "Full Planets";
            this.PlanetsCheckBox.UseVisualStyleBackColor = true;
            this.PlanetsCheckBox.Visible = false;
            // 
            // TRbutton
            // 
            this.TRbutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TRbutton.Location = new System.Drawing.Point(49, 12);
            this.TRbutton.Name = "TRbutton";
            this.TRbutton.Size = new System.Drawing.Size(190, 33);
            this.TRbutton.TabIndex = 4;
            this.TRbutton.Text = "Thrawn\'s Revenge";
            this.TRbutton.UseVisualStyleBackColor = true;
            this.TRbutton.Click += new System.EventHandler(this.TRbutton_Click);
            // 
            // FotRbutton
            // 
            this.FotRbutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FotRbutton.Location = new System.Drawing.Point(49, 51);
            this.FotRbutton.Name = "FotRbutton";
            this.FotRbutton.Size = new System.Drawing.Size(190, 33);
            this.FotRbutton.TabIndex = 5;
            this.FotRbutton.Text = "Fall of the Republic";
            this.FotRbutton.UseVisualStyleBackColor = true;
            this.FotRbutton.Click += new System.EventHandler(this.FotRbutton_Click);
            // 
            // Revbutton
            // 
            this.Revbutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Revbutton.Location = new System.Drawing.Point(49, 90);
            this.Revbutton.Name = "Revbutton";
            this.Revbutton.Size = new System.Drawing.Size(190, 33);
            this.Revbutton.TabIndex = 6;
            this.Revbutton.Text = "Revan\'s Revenge";
            this.Revbutton.UseVisualStyleBackColor = true;
            this.Revbutton.Click += new System.EventHandler(this.Revbutton_Click);
            // 
            // DevChoice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(281, 156);
            this.Controls.Add(this.Revbutton);
            this.Controls.Add(this.FotRbutton);
            this.Controls.Add(this.TRbutton);
            this.Controls.Add(this.PlanetsCheckBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DevChoice";
            this.Text = "Test Build Selection";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox PlanetsCheckBox;
        private System.Windows.Forms.Button TRbutton;
        private System.Windows.Forms.Button FotRbutton;
        private System.Windows.Forms.Button Revbutton;
    }
}