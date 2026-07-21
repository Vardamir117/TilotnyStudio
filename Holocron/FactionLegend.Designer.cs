
namespace Holocron
{
    partial class FactionLegend
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FactionLegend));
            this.TerrainCloseButton = new System.Windows.Forms.Button();
            this.FactionLegendPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.FactionLegendPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // TerrainCloseButton
            // 
            this.TerrainCloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.TerrainCloseButton.Location = new System.Drawing.Point(125, 786);
            this.TerrainCloseButton.Name = "TerrainCloseButton";
            this.TerrainCloseButton.Size = new System.Drawing.Size(75, 23);
            this.TerrainCloseButton.TabIndex = 1;
            this.TerrainCloseButton.Text = "Close";
            this.TerrainCloseButton.UseVisualStyleBackColor = true;
            this.TerrainCloseButton.Click += new System.EventHandler(this.TerrainCloseButton_Click);
            // 
            // FactionLegendPictureBox
            // 
            this.FactionLegendPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.FactionLegendPictureBox.Location = new System.Drawing.Point(12, 12);
            this.FactionLegendPictureBox.Name = "FactionLegendPictureBox";
            this.FactionLegendPictureBox.Size = new System.Drawing.Size(300, 768);
            this.FactionLegendPictureBox.TabIndex = 0;
            this.FactionLegendPictureBox.TabStop = false;
            this.FactionLegendPictureBox.Resize += new System.EventHandler(this.FactionLegendPictureBox_Resize);
            // 
            // FactionLegend
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 815);
            this.Controls.Add(this.TerrainCloseButton);
            this.Controls.Add(this.FactionLegendPictureBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FactionLegend";
            this.Text = "Faction Legend";
            this.Load += new System.EventHandler(this.FactionLegend_Load);
            ((System.ComponentModel.ISupportInitialize)(this.FactionLegendPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox FactionLegendPictureBox;
        private System.Windows.Forms.Button TerrainCloseButton;
    }
}