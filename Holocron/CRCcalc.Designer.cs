
namespace Holocron
{
    partial class CRCcalc
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CRCcalc));
            this.label1 = new System.Windows.Forms.Label();
            this.CRCInTextBox = new System.Windows.Forms.TextBox();
            this.CRCOutBox = new System.Windows.Forms.NumericUpDown();
            this.CRCclose = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.CRCOutBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter a string:";
            // 
            // CRCInTextBox
            // 
            this.CRCInTextBox.Location = new System.Drawing.Point(15, 41);
            this.CRCInTextBox.Name = "CRCInTextBox";
            this.CRCInTextBox.Size = new System.Drawing.Size(288, 20);
            this.CRCInTextBox.TabIndex = 1;
            this.CRCInTextBox.TextChanged += new System.EventHandler(this.CRCInTextBox_TextChanged);
            // 
            // CRCOutBox
            // 
            this.CRCOutBox.Location = new System.Drawing.Point(50, 68);
            this.CRCOutBox.Maximum = new decimal(new int[] {
            1569325056,
            23283064,
            0,
            0});
            this.CRCOutBox.Name = "CRCOutBox";
            this.CRCOutBox.Size = new System.Drawing.Size(207, 20);
            this.CRCOutBox.TabIndex = 2;
            // 
            // CRCclose
            // 
            this.CRCclose.Location = new System.Drawing.Point(122, 98);
            this.CRCclose.Name = "CRCclose";
            this.CRCclose.Size = new System.Drawing.Size(75, 23);
            this.CRCclose.TabIndex = 3;
            this.CRCclose.Text = "Close";
            this.CRCclose.UseVisualStyleBackColor = true;
            this.CRCclose.Click += new System.EventHandler(this.CRCclose_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "CRC:";
            // 
            // CRCcalc
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 129);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CRCclose);
            this.Controls.Add(this.CRCOutBox);
            this.Controls.Add(this.CRCInTextBox);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CRCcalc";
            this.Text = "CRC Calculator";
            ((System.ComponentModel.ISupportInitialize)(this.CRCOutBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox CRCInTextBox;
        private System.Windows.Forms.NumericUpDown CRCOutBox;
        private System.Windows.Forms.Button CRCclose;
        private System.Windows.Forms.Label label2;
    }
}