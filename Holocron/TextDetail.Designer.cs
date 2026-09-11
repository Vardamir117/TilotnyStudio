
namespace Holocron
{
    partial class TextDetail
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextDetail));
            this.DetailTextBox = new System.Windows.Forms.RichTextBox();
            this.DetailCloseButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // DetailTextBox
            // 
            this.DetailTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailTextBox.Location = new System.Drawing.Point(12, 12);
            this.DetailTextBox.Name = "DetailTextBox";
            this.DetailTextBox.Size = new System.Drawing.Size(401, 428);
            this.DetailTextBox.TabIndex = 0;
            this.DetailTextBox.Text = "";
            // 
            // DetailCloseButton
            // 
            this.DetailCloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailCloseButton.Location = new System.Drawing.Point(175, 446);
            this.DetailCloseButton.Name = "DetailCloseButton";
            this.DetailCloseButton.Size = new System.Drawing.Size(75, 23);
            this.DetailCloseButton.TabIndex = 1;
            this.DetailCloseButton.Text = "Close";
            this.DetailCloseButton.UseVisualStyleBackColor = true;
            this.DetailCloseButton.Click += new System.EventHandler(this.DetailCloseButton_Click);
            // 
            // TextDetail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 481);
            this.Controls.Add(this.DetailCloseButton);
            this.Controls.Add(this.DetailTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TextDetail";
            this.Text = "EaW:X Holocron";
            this.Load += new System.EventHandler(this.TextDetail_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox DetailTextBox;
        private System.Windows.Forms.Button DetailCloseButton;
    }
}