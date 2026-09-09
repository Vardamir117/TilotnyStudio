
namespace TilotnyStudio
{
    partial class IconPickAndAdd
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IconPickAndAdd));
            this.IconPictureBox = new System.Windows.Forms.PictureBox();
            this.IconListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.IconSearchTextBox = new System.Windows.Forms.TextBox();
            this.AddIconButton = new System.Windows.Forms.Button();
            this.AcceptButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // IconPictureBox
            // 
            this.IconPictureBox.Location = new System.Drawing.Point(704, 27);
            this.IconPictureBox.Name = "IconPictureBox";
            this.IconPictureBox.Size = new System.Drawing.Size(75, 75);
            this.IconPictureBox.TabIndex = 11;
            this.IconPictureBox.TabStop = false;
            // 
            // IconListBox
            // 
            this.IconListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.IconListBox.FormattingEnabled = true;
            this.IconListBox.Location = new System.Drawing.Point(12, 12);
            this.IconListBox.Name = "IconListBox";
            this.IconListBox.Size = new System.Drawing.Size(245, 433);
            this.IconListBox.TabIndex = 12;
            this.IconListBox.SelectedIndexChanged += new System.EventHandler(this.IconListBox_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(270, 30);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 20);
            this.label5.TabIndex = 54;
            this.label5.Text = "Search:";
            // 
            // IconSearchTextBox
            // 
            this.IconSearchTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IconSearchTextBox.Location = new System.Drawing.Point(334, 27);
            this.IconSearchTextBox.Name = "IconSearchTextBox";
            this.IconSearchTextBox.Size = new System.Drawing.Size(318, 26);
            this.IconSearchTextBox.TabIndex = 53;
            this.IconSearchTextBox.TextChanged += new System.EventHandler(this.IconSearchTextBox_TextChanged);
            // 
            // AddIconButton
            // 
            this.AddIconButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddIconButton.Location = new System.Drawing.Point(274, 71);
            this.AddIconButton.Name = "AddIconButton";
            this.AddIconButton.Size = new System.Drawing.Size(115, 31);
            this.AddIconButton.TabIndex = 55;
            this.AddIconButton.Text = "Add Icon...";
            this.AddIconButton.UseVisualStyleBackColor = true;
            // 
            // AcceptButton
            // 
            this.AcceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AcceptButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AcceptButton.Location = new System.Drawing.Point(673, 414);
            this.AcceptButton.Name = "AcceptButton";
            this.AcceptButton.Size = new System.Drawing.Size(115, 31);
            this.AcceptButton.TabIndex = 56;
            this.AcceptButton.Text = "Accept";
            this.AcceptButton.UseVisualStyleBackColor = true;
            this.AcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelButton.Location = new System.Drawing.Point(552, 414);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(115, 31);
            this.CancelButton.TabIndex = 57;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // IconPickAndAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.AcceptButton);
            this.Controls.Add(this.AddIconButton);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.IconSearchTextBox);
            this.Controls.Add(this.IconListBox);
            this.Controls.Add(this.IconPictureBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "IconPickAndAdd";
            this.Text = "Pick Icon";
            this.Load += new System.EventHandler(this.IconPickAndAdd_Load);
            ((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox IconPictureBox;
        private System.Windows.Forms.ListBox IconListBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox IconSearchTextBox;
        private System.Windows.Forms.Button AddIconButton;
        private System.Windows.Forms.Button AcceptButton;
        private System.Windows.Forms.Button CancelButton;
    }
}