
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
            this.IconAcceptButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.SelectedIconLabel = new System.Windows.Forms.Label();
            this.UsedMapPictureBox = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.UsedMapPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // IconPictureBox
            // 
            this.IconPictureBox.Location = new System.Drawing.Point(673, 27);
            this.IconPictureBox.Name = "IconPictureBox";
            this.IconPictureBox.Size = new System.Drawing.Size(179, 172);
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
            this.AddIconButton.Click += new System.EventHandler(this.AddIconButton_Click);
            // 
            // IconAcceptButton
            // 
            this.IconAcceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.IconAcceptButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IconAcceptButton.Location = new System.Drawing.Point(737, 430);
            this.IconAcceptButton.Name = "IconAcceptButton";
            this.IconAcceptButton.Size = new System.Drawing.Size(115, 31);
            this.IconAcceptButton.TabIndex = 56;
            this.IconAcceptButton.Text = "Accept";
            this.IconAcceptButton.UseVisualStyleBackColor = true;
            this.IconAcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelButton.Location = new System.Drawing.Point(616, 430);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(115, 31);
            this.CancelButton.TabIndex = 57;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // SelectedIconLabel
            // 
            this.SelectedIconLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SelectedIconLabel.AutoSize = true;
            this.SelectedIconLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SelectedIconLabel.Location = new System.Drawing.Point(283, 392);
            this.SelectedIconLabel.Name = "SelectedIconLabel";
            this.SelectedIconLabel.Size = new System.Drawing.Size(76, 20);
            this.SelectedIconLabel.TabIndex = 58;
            this.SelectedIconLabel.Text = "Selected:";
            // 
            // UsedMapPictureBox
            // 
            this.UsedMapPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UsedMapPictureBox.Location = new System.Drawing.Point(274, 120);
            this.UsedMapPictureBox.Name = "UsedMapPictureBox";
            this.UsedMapPictureBox.Size = new System.Drawing.Size(393, 269);
            this.UsedMapPictureBox.TabIndex = 59;
            this.UsedMapPictureBox.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(271, 105);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 13);
            this.label1.TabIndex = 60;
            this.label1.Text = "Red areas are allocated to icons";
            // 
            // IconPickAndAdd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 466);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.UsedMapPictureBox);
            this.Controls.Add(this.SelectedIconLabel);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.IconAcceptButton);
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
            ((System.ComponentModel.ISupportInitialize)(this.UsedMapPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox IconPictureBox;
        private System.Windows.Forms.ListBox IconListBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox IconSearchTextBox;
        private System.Windows.Forms.Button AddIconButton;
        private System.Windows.Forms.Button IconAcceptButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label SelectedIconLabel;
        private System.Windows.Forms.PictureBox UsedMapPictureBox;
        private System.Windows.Forms.Label label1;
    }
}