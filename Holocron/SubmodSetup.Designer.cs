namespace Holocron
{
    partial class SubmodSetup
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubmodSetup));
            this.SubmodButton = new System.Windows.Forms.Button();
            this.WorkshopListbox = new System.Windows.Forms.ListBox();
            this.LocalListbox = new System.Windows.Forms.ListBox();
            this.ShortcutLabel = new System.Windows.Forms.Label();
            this.WorkshopButton = new System.Windows.Forms.Button();
            this.LocalButton = new System.Windows.Forms.Button();
            this.ReloadButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ClearButton = new System.Windows.Forms.Button();
            this.CloseButton = new System.Windows.Forms.Button();
            this.FoCButton = new System.Windows.Forms.Button();
            this.EaWButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // SubmodButton
            // 
            this.SubmodButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmodButton.Location = new System.Drawing.Point(178, 346);
            this.SubmodButton.Name = "SubmodButton";
            this.SubmodButton.Size = new System.Drawing.Size(174, 30);
            this.SubmodButton.TabIndex = 0;
            this.SubmodButton.Text = "Create Shortcut...";
            this.SubmodButton.UseVisualStyleBackColor = true;
            this.SubmodButton.Click += new System.EventHandler(this.SubmodButton_Click);
            // 
            // WorkshopListbox
            // 
            this.WorkshopListbox.FormattingEnabled = true;
            this.WorkshopListbox.Location = new System.Drawing.Point(16, 43);
            this.WorkshopListbox.Name = "WorkshopListbox";
            this.WorkshopListbox.Size = new System.Drawing.Size(336, 160);
            this.WorkshopListbox.TabIndex = 1;
            // 
            // LocalListbox
            // 
            this.LocalListbox.FormattingEnabled = true;
            this.LocalListbox.Location = new System.Drawing.Point(386, 43);
            this.LocalListbox.Name = "LocalListbox";
            this.LocalListbox.Size = new System.Drawing.Size(336, 160);
            this.LocalListbox.TabIndex = 2;
            // 
            // ShortcutLabel
            // 
            this.ShortcutLabel.AutoSize = true;
            this.ShortcutLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ShortcutLabel.Location = new System.Drawing.Point(12, 303);
            this.ShortcutLabel.Name = "ShortcutLabel";
            this.ShortcutLabel.Size = new System.Drawing.Size(51, 20);
            this.ShortcutLabel.TabIndex = 3;
            this.ShortcutLabel.Text = "label1";
            // 
            // WorkshopButton
            // 
            this.WorkshopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WorkshopButton.Location = new System.Drawing.Point(16, 218);
            this.WorkshopButton.Name = "WorkshopButton";
            this.WorkshopButton.Size = new System.Drawing.Size(336, 30);
            this.WorkshopButton.TabIndex = 4;
            this.WorkshopButton.Text = "Add Workshop Mod";
            this.WorkshopButton.UseVisualStyleBackColor = true;
            this.WorkshopButton.Click += new System.EventHandler(this.WorkshopButton_Click);
            // 
            // LocalButton
            // 
            this.LocalButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LocalButton.Location = new System.Drawing.Point(386, 218);
            this.LocalButton.Name = "LocalButton";
            this.LocalButton.Size = new System.Drawing.Size(336, 30);
            this.LocalButton.TabIndex = 5;
            this.LocalButton.Text = "Add Local Mod";
            this.LocalButton.UseVisualStyleBackColor = true;
            this.LocalButton.Click += new System.EventHandler(this.LocalButton_Click);
            // 
            // ReloadButton
            // 
            this.ReloadButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReloadButton.Location = new System.Drawing.Point(386, 346);
            this.ReloadButton.Name = "ReloadButton";
            this.ReloadButton.Size = new System.Drawing.Size(204, 30);
            this.ReloadButton.TabIndex = 6;
            this.ReloadButton.Text = "Reload Holocron Data";
            this.ReloadButton.UseVisualStyleBackColor = true;
            this.ReloadButton.Click += new System.EventHandler(this.ReloadButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(224, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Load highest priority mods first";
            // 
            // ClearButton
            // 
            this.ClearButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClearButton.Location = new System.Drawing.Point(12, 346);
            this.ClearButton.Name = "ClearButton";
            this.ClearButton.Size = new System.Drawing.Size(139, 30);
            this.ClearButton.TabIndex = 8;
            this.ClearButton.Text = "Clear Mod Stack";
            this.ClearButton.UseVisualStyleBackColor = true;
            this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // CloseButton
            // 
            this.CloseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseButton.Location = new System.Drawing.Point(611, 346);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(111, 30);
            this.CloseButton.TabIndex = 9;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // FoCButton
            // 
            this.FoCButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FoCButton.Location = new System.Drawing.Point(16, 254);
            this.FoCButton.Name = "FoCButton";
            this.FoCButton.Size = new System.Drawing.Size(336, 30);
            this.FoCButton.TabIndex = 10;
            this.FoCButton.Text = "Add Forces of Corruption";
            this.FoCButton.UseVisualStyleBackColor = true;
            this.FoCButton.Click += new System.EventHandler(this.FoCButton_Click);
            // 
            // EaWButton
            // 
            this.EaWButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EaWButton.Location = new System.Drawing.Point(386, 254);
            this.EaWButton.Name = "EaWButton";
            this.EaWButton.Size = new System.Drawing.Size(336, 30);
            this.EaWButton.TabIndex = 11;
            this.EaWButton.Text = "Add Empire at War";
            this.EaWButton.UseVisualStyleBackColor = true;
            this.EaWButton.Click += new System.EventHandler(this.EaWButton_Click);
            // 
            // SubmodSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 398);
            this.Controls.Add(this.EaWButton);
            this.Controls.Add(this.FoCButton);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.ClearButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ReloadButton);
            this.Controls.Add(this.LocalButton);
            this.Controls.Add(this.WorkshopButton);
            this.Controls.Add(this.ShortcutLabel);
            this.Controls.Add(this.LocalListbox);
            this.Controls.Add(this.WorkshopListbox);
            this.Controls.Add(this.SubmodButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SubmodSetup";
            this.Text = "Data File Setup";
            this.Load += new System.EventHandler(this.SubmodSetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SubmodButton;
        private System.Windows.Forms.ListBox WorkshopListbox;
        private System.Windows.Forms.ListBox LocalListbox;
        private System.Windows.Forms.Label ShortcutLabel;
        private System.Windows.Forms.Button WorkshopButton;
        private System.Windows.Forms.Button LocalButton;
        private System.Windows.Forms.Button ReloadButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button FoCButton;
        private System.Windows.Forms.Button EaWButton;
    }
}