
namespace Holocron
{
    partial class TerrainLegend
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TerrainLegend));
            this.TerrainCloseButton = new System.Windows.Forms.Button();
            this.TerrainLegendPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.TerrainLegendPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // TerrainCloseButton
            // 
            this.TerrainCloseButton.Location = new System.Drawing.Point(73, 320);
            this.TerrainCloseButton.Name = "TerrainCloseButton";
            this.TerrainCloseButton.Size = new System.Drawing.Size(75, 23);
            this.TerrainCloseButton.TabIndex = 1;
            this.TerrainCloseButton.Text = "Close";
            this.TerrainCloseButton.UseVisualStyleBackColor = true;
            this.TerrainCloseButton.Click += new System.EventHandler(this.TerrainCloseButton_Click);
            // 
            // TerrainLegendPictureBox
            // 
            this.TerrainLegendPictureBox.Location = new System.Drawing.Point(12, 12);
            this.TerrainLegendPictureBox.Name = "TerrainLegendPictureBox";
            this.TerrainLegendPictureBox.Size = new System.Drawing.Size(196, 302);
            this.TerrainLegendPictureBox.TabIndex = 0;
            this.TerrainLegendPictureBox.TabStop = false;
            // 
            // TerrainLegend
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(220, 354);
            this.Controls.Add(this.TerrainCloseButton);
            this.Controls.Add(this.TerrainLegendPictureBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TerrainLegend";
            this.Text = "Terrain Legend";
            this.Load += new System.EventHandler(this.TerrainLegend_Load);
            ((System.ComponentModel.ISupportInitialize)(this.TerrainLegendPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox TerrainLegendPictureBox;
        private System.Windows.Forms.Button TerrainCloseButton;
    }
}