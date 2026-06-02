namespace Holocron
{
    partial class UnitSort
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UnitSort));
            this.NameRB = new System.Windows.Forms.RadioButton();
            this.AscComboBox = new System.Windows.Forms.ComboBox();
            this.PriceRB = new System.Windows.Forms.RadioButton();
            this.PopRB = new System.Windows.Forms.RadioButton();
            this.BuildTimeRB = new System.Windows.Forms.RadioButton();
            this.DenomComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cpRB = new System.Windows.Forms.RadioButton();
            this.CompanySizeRB = new System.Windows.Forms.RadioButton();
            this.ATypeRB = new System.Windows.Forms.RadioButton();
            this.STypeRB = new System.Windows.Forms.RadioButton();
            this.ClassRB = new System.Windows.Forms.RadioButton();
            this.DurabilityRB = new System.Windows.Forms.RadioButton();
            this.RegenRB = new System.Windows.Forms.RadioButton();
            this.SpeedRB = new System.Windows.Forms.RadioButton();
            this.dpsRawRB = new System.Windows.Forms.RadioButton();
            this.ShieldRB = new System.Windows.Forms.RadioButton();
            this.hpRB = new System.Windows.Forms.RadioButton();
            this.AccelRB = new System.Windows.Forms.RadioButton();
            this.TurnRB = new System.Windows.Forms.RadioButton();
            this.dpsAvgRB = new System.Windows.Forms.RadioButton();
            this.dpsArmorRB = new System.Windows.Forms.RadioButton();
            this.UnitSortAcceptButton = new System.Windows.Forms.Button();
            this.UnitSortCancelButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.SkPriceRB = new System.Windows.Forms.RadioButton();
            this.SkBuildTimeRB = new System.Windows.Forms.RadioButton();
            this.dpsShieldRB = new System.Windows.Forms.RadioButton();
            this.DurabilityBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.InternalRB = new System.Windows.Forms.RadioButton();
            this.ComplementRB = new System.Windows.Forms.RadioButton();
            this.FighterTypeBox = new System.Windows.Forms.ComboBox();
            this.ReserveBox = new System.Windows.Forms.ComboBox();
            this.AccuracyCheckBox = new System.Windows.Forms.CheckBox();
            this.GarrisonValueRB = new System.Windows.Forms.RadioButton();
            this.GarrisonSlotsRB = new System.Windows.Forms.RadioButton();
            this.ShipyardRB = new System.Windows.Forms.RadioButton();
            this.CrewRB = new System.Windows.Forms.RadioButton();
            this.ComplementCheckBox = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ClearButton = new System.Windows.Forms.Button();
            this.NameCountRB = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // NameRB
            // 
            this.NameRB.AutoSize = true;
            this.NameRB.Location = new System.Drawing.Point(15, 33);
            this.NameRB.Name = "NameRB";
            this.NameRB.Size = new System.Drawing.Size(53, 17);
            this.NameRB.TabIndex = 0;
            this.NameRB.TabStop = true;
            this.NameRB.Text = "Name";
            this.NameRB.UseVisualStyleBackColor = true;
            // 
            // AscComboBox
            // 
            this.AscComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.AscComboBox.FormattingEnabled = true;
            this.AscComboBox.Items.AddRange(new object[] {
            "Ascending",
            "Descending"});
            this.AscComboBox.Location = new System.Drawing.Point(639, 29);
            this.AscComboBox.Name = "AscComboBox";
            this.AscComboBox.Size = new System.Drawing.Size(121, 21);
            this.AscComboBox.TabIndex = 1;
            // 
            // PriceRB
            // 
            this.PriceRB.AutoSize = true;
            this.PriceRB.Location = new System.Drawing.Point(15, 121);
            this.PriceRB.Name = "PriceRB";
            this.PriceRB.Size = new System.Drawing.Size(49, 17);
            this.PriceRB.TabIndex = 2;
            this.PriceRB.TabStop = true;
            this.PriceRB.Text = "Price";
            this.PriceRB.UseVisualStyleBackColor = true;
            // 
            // PopRB
            // 
            this.PopRB.AutoSize = true;
            this.PopRB.Location = new System.Drawing.Point(15, 55);
            this.PopRB.Name = "PopRB";
            this.PopRB.Size = new System.Drawing.Size(75, 17);
            this.PopRB.TabIndex = 3;
            this.PopRB.TabStop = true;
            this.PopRB.Text = "Population";
            this.PopRB.UseVisualStyleBackColor = true;
            // 
            // BuildTimeRB
            // 
            this.BuildTimeRB.AutoSize = true;
            this.BuildTimeRB.Location = new System.Drawing.Point(15, 165);
            this.BuildTimeRB.Name = "BuildTimeRB";
            this.BuildTimeRB.Size = new System.Drawing.Size(74, 17);
            this.BuildTimeRB.TabIndex = 4;
            this.BuildTimeRB.TabStop = true;
            this.BuildTimeRB.Text = "Build Time";
            this.BuildTimeRB.UseVisualStyleBackColor = true;
            // 
            // DenomComboBox
            // 
            this.DenomComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DenomComboBox.FormattingEnabled = true;
            this.DenomComboBox.Items.AddRange(new object[] {
            "Absolute Value",
            "Per Combat Power",
            "Per Population",
            "Per Credit",
            "Per Skirmish Price",
            "Per Build Time",
            "Per Skirmish Time",
            "Per Unit in Company",
            "Per Crew"});
            this.DenomComboBox.Location = new System.Drawing.Point(479, 29);
            this.DenomComboBox.Name = "DenomComboBox";
            this.DenomComboBox.Size = new System.Drawing.Size(154, 21);
            this.DenomComboBox.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Sort by:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(475, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Normalize by:";
            // 
            // cpRB
            // 
            this.cpRB.AutoSize = true;
            this.cpRB.Location = new System.Drawing.Point(171, 11);
            this.cpRB.Name = "cpRB";
            this.cpRB.Size = new System.Drawing.Size(94, 17);
            this.cpRB.TabIndex = 8;
            this.cpRB.TabStop = true;
            this.cpRB.Text = "Combat Power";
            this.cpRB.UseVisualStyleBackColor = true;
            // 
            // CompanySizeRB
            // 
            this.CompanySizeRB.AutoSize = true;
            this.CompanySizeRB.Location = new System.Drawing.Point(15, 209);
            this.CompanySizeRB.Name = "CompanySizeRB";
            this.CompanySizeRB.Size = new System.Drawing.Size(92, 17);
            this.CompanySizeRB.TabIndex = 9;
            this.CompanySizeRB.TabStop = true;
            this.CompanySizeRB.Text = "Company Size";
            this.CompanySizeRB.UseVisualStyleBackColor = true;
            this.CompanySizeRB.Visible = false;
            // 
            // ATypeRB
            // 
            this.ATypeRB.AutoSize = true;
            this.ATypeRB.Location = new System.Drawing.Point(171, 143);
            this.ATypeRB.Name = "ATypeRB";
            this.ATypeRB.Size = new System.Drawing.Size(79, 17);
            this.ATypeRB.TabIndex = 10;
            this.ATypeRB.TabStop = true;
            this.ATypeRB.Text = "Armor Type";
            this.ATypeRB.UseVisualStyleBackColor = true;
            // 
            // STypeRB
            // 
            this.STypeRB.AutoSize = true;
            this.STypeRB.Location = new System.Drawing.Point(171, 121);
            this.STypeRB.Name = "STypeRB";
            this.STypeRB.Size = new System.Drawing.Size(81, 17);
            this.STypeRB.TabIndex = 11;
            this.STypeRB.TabStop = true;
            this.STypeRB.Text = "Shield Type";
            this.STypeRB.UseVisualStyleBackColor = true;
            // 
            // ClassRB
            // 
            this.ClassRB.AutoSize = true;
            this.ClassRB.Location = new System.Drawing.Point(15, 100);
            this.ClassRB.Name = "ClassRB";
            this.ClassRB.Size = new System.Drawing.Size(50, 17);
            this.ClassRB.TabIndex = 12;
            this.ClassRB.TabStop = true;
            this.ClassRB.Text = "Class";
            this.ClassRB.UseVisualStyleBackColor = true;
            // 
            // DurabilityRB
            // 
            this.DurabilityRB.AutoSize = true;
            this.DurabilityRB.Location = new System.Drawing.Point(171, 33);
            this.DurabilityRB.Name = "DurabilityRB";
            this.DurabilityRB.Size = new System.Drawing.Size(96, 17);
            this.DurabilityRB.TabIndex = 14;
            this.DurabilityRB.TabStop = true;
            this.DurabilityRB.Text = "Shields+Health";
            this.DurabilityRB.UseVisualStyleBackColor = true;
            // 
            // RegenRB
            // 
            this.RegenRB.AutoSize = true;
            this.RegenRB.Location = new System.Drawing.Point(171, 99);
            this.RegenRB.Name = "RegenRB";
            this.RegenRB.Size = new System.Drawing.Size(121, 17);
            this.RegenRB.TabIndex = 16;
            this.RegenRB.TabStop = true;
            this.RegenRB.Text = "Shield Regeneration";
            this.RegenRB.UseVisualStyleBackColor = true;
            // 
            // SpeedRB
            // 
            this.SpeedRB.AutoSize = true;
            this.SpeedRB.Location = new System.Drawing.Point(171, 165);
            this.SpeedRB.Name = "SpeedRB";
            this.SpeedRB.Size = new System.Drawing.Size(56, 17);
            this.SpeedRB.TabIndex = 17;
            this.SpeedRB.TabStop = true;
            this.SpeedRB.Text = "Speed";
            this.SpeedRB.UseVisualStyleBackColor = true;
            // 
            // dpsRawRB
            // 
            this.dpsRawRB.AutoSize = true;
            this.dpsRawRB.Location = new System.Drawing.Point(171, 231);
            this.dpsRawRB.Name = "dpsRawRB";
            this.dpsRawRB.Size = new System.Drawing.Size(163, 17);
            this.dpsRawRB.TabIndex = 18;
            this.dpsRawRB.TabStop = true;
            this.dpsRawRB.Text = "Damage Per Second of Type";
            this.dpsRawRB.UseVisualStyleBackColor = true;
            // 
            // ShieldRB
            // 
            this.ShieldRB.AutoSize = true;
            this.ShieldRB.Location = new System.Drawing.Point(171, 77);
            this.ShieldRB.Name = "ShieldRB";
            this.ShieldRB.Size = new System.Drawing.Size(59, 17);
            this.ShieldRB.TabIndex = 23;
            this.ShieldRB.TabStop = true;
            this.ShieldRB.Text = "Shields";
            this.ShieldRB.UseVisualStyleBackColor = true;
            // 
            // hpRB
            // 
            this.hpRB.AutoSize = true;
            this.hpRB.Location = new System.Drawing.Point(171, 55);
            this.hpRB.Name = "hpRB";
            this.hpRB.Size = new System.Drawing.Size(56, 17);
            this.hpRB.TabIndex = 24;
            this.hpRB.TabStop = true;
            this.hpRB.Text = "Health";
            this.hpRB.UseVisualStyleBackColor = true;
            // 
            // AccelRB
            // 
            this.AccelRB.AutoSize = true;
            this.AccelRB.Location = new System.Drawing.Point(171, 187);
            this.AccelRB.Name = "AccelRB";
            this.AccelRB.Size = new System.Drawing.Size(84, 17);
            this.AccelRB.TabIndex = 25;
            this.AccelRB.TabStop = true;
            this.AccelRB.Text = "Acceleration";
            this.AccelRB.UseVisualStyleBackColor = true;
            // 
            // TurnRB
            // 
            this.TurnRB.AutoSize = true;
            this.TurnRB.Location = new System.Drawing.Point(171, 209);
            this.TurnRB.Name = "TurnRB";
            this.TurnRB.Size = new System.Drawing.Size(47, 17);
            this.TurnRB.TabIndex = 26;
            this.TurnRB.TabStop = true;
            this.TurnRB.Text = "Turn";
            this.TurnRB.UseVisualStyleBackColor = true;
            // 
            // dpsAvgRB
            // 
            this.dpsAvgRB.AutoSize = true;
            this.dpsAvgRB.Location = new System.Drawing.Point(171, 253);
            this.dpsAvgRB.Name = "dpsAvgRB";
            this.dpsAvgRB.Size = new System.Drawing.Size(173, 17);
            this.dpsAvgRB.TabIndex = 27;
            this.dpsAvgRB.TabStop = true;
            this.dpsAvgRB.Text = "Damage Per Second Averaged";
            this.dpsAvgRB.UseVisualStyleBackColor = true;
            // 
            // dpsArmorRB
            // 
            this.dpsArmorRB.AutoSize = true;
            this.dpsArmorRB.Location = new System.Drawing.Point(171, 275);
            this.dpsArmorRB.Name = "dpsArmorRB";
            this.dpsArmorRB.Size = new System.Drawing.Size(188, 17);
            this.dpsArmorRB.TabIndex = 28;
            this.dpsArmorRB.TabStop = true;
            this.dpsArmorRB.Text = "Damage Per Second Target Armor";
            this.dpsArmorRB.UseVisualStyleBackColor = true;
            // 
            // UnitSortAcceptButton
            // 
            this.UnitSortAcceptButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UnitSortAcceptButton.Location = new System.Drawing.Point(514, 369);
            this.UnitSortAcceptButton.Name = "UnitSortAcceptButton";
            this.UnitSortAcceptButton.Size = new System.Drawing.Size(104, 29);
            this.UnitSortAcceptButton.TabIndex = 29;
            this.UnitSortAcceptButton.Text = "Accept";
            this.UnitSortAcceptButton.UseVisualStyleBackColor = true;
            this.UnitSortAcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // UnitSortCancelButton
            // 
            this.UnitSortCancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UnitSortCancelButton.Location = new System.Drawing.Point(639, 369);
            this.UnitSortCancelButton.Name = "UnitSortCancelButton";
            this.UnitSortCancelButton.Size = new System.Drawing.Size(104, 29);
            this.UnitSortCancelButton.TabIndex = 30;
            this.UnitSortCancelButton.Text = "Cancel";
            this.UnitSortCancelButton.UseVisualStyleBackColor = true;
            this.UnitSortCancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(361, 255);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(126, 13);
            this.label4.TabIndex = 32;
            this.label4.Text = "(averaged over modifiers)";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(361, 277);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(132, 13);
            this.label5.TabIndex = 33;
            this.label5.Text = "(for chosen target settings)";
            // 
            // SkPriceRB
            // 
            this.SkPriceRB.AutoSize = true;
            this.SkPriceRB.Location = new System.Drawing.Point(15, 143);
            this.SkPriceRB.Name = "SkPriceRB";
            this.SkPriceRB.Size = new System.Drawing.Size(91, 17);
            this.SkPriceRB.TabIndex = 34;
            this.SkPriceRB.TabStop = true;
            this.SkPriceRB.Text = "Skirmish Price";
            this.SkPriceRB.UseVisualStyleBackColor = true;
            // 
            // SkBuildTimeRB
            // 
            this.SkBuildTimeRB.AutoSize = true;
            this.SkBuildTimeRB.Location = new System.Drawing.Point(15, 187);
            this.SkBuildTimeRB.Name = "SkBuildTimeRB";
            this.SkBuildTimeRB.Size = new System.Drawing.Size(116, 17);
            this.SkBuildTimeRB.TabIndex = 35;
            this.SkBuildTimeRB.TabStop = true;
            this.SkBuildTimeRB.Text = "Skirmish Build Time";
            this.SkBuildTimeRB.UseVisualStyleBackColor = true;
            // 
            // dpsShieldRB
            // 
            this.dpsShieldRB.AutoSize = true;
            this.dpsShieldRB.Location = new System.Drawing.Point(171, 297);
            this.dpsShieldRB.Name = "dpsShieldRB";
            this.dpsShieldRB.Size = new System.Drawing.Size(190, 17);
            this.dpsShieldRB.TabIndex = 36;
            this.dpsShieldRB.TabStop = true;
            this.dpsShieldRB.Text = "Damage Per Second Target Shield\r\n";
            this.dpsShieldRB.UseVisualStyleBackColor = true;
            // 
            // DurabilityBox
            // 
            this.DurabilityBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DurabilityBox.FormattingEnabled = true;
            this.DurabilityBox.Items.AddRange(new object[] {
            "Base Value",
            "Base+Reflect and Absorb",
            "Average Modified",
            "On Incoming Damage"});
            this.DurabilityBox.Location = new System.Drawing.Point(273, 32);
            this.DurabilityBox.Name = "DurabilityBox";
            this.DurabilityBox.Size = new System.Drawing.Size(121, 21);
            this.DurabilityBox.TabIndex = 38;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(273, 60);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(242, 26);
            this.label6.TabIndex = 39;
            this.label6.Text = "Applies to all health and shield values\r\nTargeted Modifed follows Incoming Damage" +
    " Type";
            // 
            // InternalRB
            // 
            this.InternalRB.AutoSize = true;
            this.InternalRB.Location = new System.Drawing.Point(15, 77);
            this.InternalRB.Name = "InternalRB";
            this.InternalRB.Size = new System.Drawing.Size(91, 17);
            this.InternalRB.TabIndex = 40;
            this.InternalRB.TabStop = true;
            this.InternalRB.Text = "Internal Name";
            this.InternalRB.UseVisualStyleBackColor = true;
            // 
            // ComplementRB
            // 
            this.ComplementRB.AutoSize = true;
            this.ComplementRB.Location = new System.Drawing.Point(171, 318);
            this.ComplementRB.Name = "ComplementRB";
            this.ComplementRB.Size = new System.Drawing.Size(83, 17);
            this.ComplementRB.TabIndex = 42;
            this.ComplementRB.TabStop = true;
            this.ComplementRB.Text = "Complement";
            this.ComplementRB.UseVisualStyleBackColor = true;
            this.ComplementRB.Visible = false;
            // 
            // FighterTypeBox
            // 
            this.FighterTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FighterTypeBox.FormattingEnabled = true;
            this.FighterTypeBox.Items.AddRange(new object[] {
            "Fighter Count",
            "Bomber Count",
            "Fighters and Bomber Count",
            "Combat Power"});
            this.FighterTypeBox.Location = new System.Drawing.Point(273, 318);
            this.FighterTypeBox.Name = "FighterTypeBox";
            this.FighterTypeBox.Size = new System.Drawing.Size(121, 21);
            this.FighterTypeBox.TabIndex = 43;
            this.FighterTypeBox.Visible = false;
            // 
            // ReserveBox
            // 
            this.ReserveBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ReserveBox.FormattingEnabled = true;
            this.ReserveBox.Items.AddRange(new object[] {
            "Upfront",
            "Reserve",
            "Upfront+Reserve"});
            this.ReserveBox.Location = new System.Drawing.Point(425, 318);
            this.ReserveBox.Name = "ReserveBox";
            this.ReserveBox.Size = new System.Drawing.Size(121, 21);
            this.ReserveBox.TabIndex = 44;
            this.ReserveBox.Visible = false;
            // 
            // AccuracyCheckBox
            // 
            this.AccuracyCheckBox.AutoSize = true;
            this.AccuracyCheckBox.Location = new System.Drawing.Point(364, 298);
            this.AccuracyCheckBox.Name = "AccuracyCheckBox";
            this.AccuracyCheckBox.Size = new System.Drawing.Size(145, 17);
            this.AccuracyCheckBox.TabIndex = 41;
            this.AccuracyCheckBox.Text = "Include Accuracy in DPS";
            this.AccuracyCheckBox.UseVisualStyleBackColor = true;
            // 
            // GarrisonValueRB
            // 
            this.GarrisonValueRB.AutoSize = true;
            this.GarrisonValueRB.Location = new System.Drawing.Point(171, 340);
            this.GarrisonValueRB.Name = "GarrisonValueRB";
            this.GarrisonValueRB.Size = new System.Drawing.Size(150, 17);
            this.GarrisonValueRB.TabIndex = 45;
            this.GarrisonValueRB.TabStop = true;
            this.GarrisonValueRB.Text = "Garrison pips when loaded";
            this.GarrisonValueRB.UseVisualStyleBackColor = true;
            // 
            // GarrisonSlotsRB
            // 
            this.GarrisonSlotsRB.AutoSize = true;
            this.GarrisonSlotsRB.Location = new System.Drawing.Point(103, 318);
            this.GarrisonSlotsRB.Name = "GarrisonSlotsRB";
            this.GarrisonSlotsRB.Size = new System.Drawing.Size(108, 17);
            this.GarrisonSlotsRB.TabIndex = 46;
            this.GarrisonSlotsRB.TabStop = true;
            this.GarrisonSlotsRB.Text = "Garrison Capacity";
            this.GarrisonSlotsRB.UseVisualStyleBackColor = true;
            // 
            // ShipyardRB
            // 
            this.ShipyardRB.AutoSize = true;
            this.ShipyardRB.Location = new System.Drawing.Point(57, 209);
            this.ShipyardRB.Name = "ShipyardRB";
            this.ShipyardRB.Size = new System.Drawing.Size(95, 17);
            this.ShipyardRB.TabIndex = 47;
            this.ShipyardRB.TabStop = true;
            this.ShipyardRB.Text = "Shipyard Level";
            this.ShipyardRB.UseVisualStyleBackColor = true;
            this.ShipyardRB.Visible = false;
            // 
            // CrewRB
            // 
            this.CrewRB.AutoSize = true;
            this.CrewRB.Location = new System.Drawing.Point(15, 251);
            this.CrewRB.Name = "CrewRB";
            this.CrewRB.Size = new System.Drawing.Size(49, 17);
            this.CrewRB.TabIndex = 13;
            this.CrewRB.TabStop = true;
            this.CrewRB.Text = "Crew";
            this.CrewRB.UseVisualStyleBackColor = true;
            this.CrewRB.Visible = false;
            // 
            // ComplementCheckBox
            // 
            this.ComplementCheckBox.AutoSize = true;
            this.ComplementCheckBox.Location = new System.Drawing.Point(272, 13);
            this.ComplementCheckBox.Name = "ComplementCheckBox";
            this.ComplementCheckBox.Size = new System.Drawing.Size(122, 17);
            this.ComplementCheckBox.TabIndex = 49;
            this.ComplementCheckBox.Text = "Include Complement";
            this.ComplementCheckBox.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(361, 235);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(162, 13);
            this.label3.TabIndex = 50;
            this.label3.Text = "(controlled by Incoming Damage)";
            // 
            // ClearButton
            // 
            this.ClearButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClearButton.Location = new System.Drawing.Point(383, 369);
            this.ClearButton.Name = "ClearButton";
            this.ClearButton.Size = new System.Drawing.Size(104, 29);
            this.ClearButton.TabIndex = 51;
            this.ClearButton.Text = "Clear Config";
            this.ClearButton.UseVisualStyleBackColor = true;
            this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // NameCountRB
            // 
            this.NameCountRB.AutoSize = true;
            this.NameCountRB.Location = new System.Drawing.Point(15, 230);
            this.NameCountRB.Name = "NameCountRB";
            this.NameCountRB.Size = new System.Drawing.Size(118, 17);
            this.NameCountRB.TabIndex = 52;
            this.NameCountRB.TabStop = true;
            this.NameCountRB.Text = "Name Count TODO";
            this.NameCountRB.UseVisualStyleBackColor = true;
            this.NameCountRB.Visible = false;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(57, 251);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(153, 17);
            this.radioButton1.TabIndex = 53;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Variant (Chain Start) TODO";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.Visible = false;
            // 
            // UnitSort
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(767, 408);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.NameCountRB);
            this.Controls.Add(this.ClearButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ComplementCheckBox);
            this.Controls.Add(this.GarrisonValueRB);
            this.Controls.Add(this.ReserveBox);
            this.Controls.Add(this.FighterTypeBox);
            this.Controls.Add(this.ComplementRB);
            this.Controls.Add(this.AccuracyCheckBox);
            this.Controls.Add(this.InternalRB);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.DurabilityBox);
            this.Controls.Add(this.dpsShieldRB);
            this.Controls.Add(this.SkBuildTimeRB);
            this.Controls.Add(this.SkPriceRB);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.UnitSortCancelButton);
            this.Controls.Add(this.UnitSortAcceptButton);
            this.Controls.Add(this.dpsArmorRB);
            this.Controls.Add(this.dpsAvgRB);
            this.Controls.Add(this.TurnRB);
            this.Controls.Add(this.AccelRB);
            this.Controls.Add(this.hpRB);
            this.Controls.Add(this.ShieldRB);
            this.Controls.Add(this.dpsRawRB);
            this.Controls.Add(this.SpeedRB);
            this.Controls.Add(this.RegenRB);
            this.Controls.Add(this.DurabilityRB);
            this.Controls.Add(this.CrewRB);
            this.Controls.Add(this.ClassRB);
            this.Controls.Add(this.STypeRB);
            this.Controls.Add(this.ATypeRB);
            this.Controls.Add(this.CompanySizeRB);
            this.Controls.Add(this.cpRB);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DenomComboBox);
            this.Controls.Add(this.BuildTimeRB);
            this.Controls.Add(this.PopRB);
            this.Controls.Add(this.PriceRB);
            this.Controls.Add(this.AscComboBox);
            this.Controls.Add(this.NameRB);
            this.Controls.Add(this.GarrisonSlotsRB);
            this.Controls.Add(this.ShipyardRB);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UnitSort";
            this.Text = "Unit Sort Configuration";
            this.Load += new System.EventHandler(this.UserSort_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton NameRB;
        private System.Windows.Forms.ComboBox AscComboBox;
        private System.Windows.Forms.RadioButton PriceRB;
        private System.Windows.Forms.RadioButton PopRB;
        private System.Windows.Forms.RadioButton BuildTimeRB;
        private System.Windows.Forms.ComboBox DenomComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton cpRB;
        private System.Windows.Forms.RadioButton CompanySizeRB;
        private System.Windows.Forms.RadioButton ATypeRB;
        private System.Windows.Forms.RadioButton STypeRB;
        private System.Windows.Forms.RadioButton ClassRB;
        private System.Windows.Forms.RadioButton DurabilityRB;
        private System.Windows.Forms.RadioButton RegenRB;
        private System.Windows.Forms.RadioButton SpeedRB;
        private System.Windows.Forms.RadioButton dpsRawRB;
        private System.Windows.Forms.RadioButton ShieldRB;
        private System.Windows.Forms.RadioButton hpRB;
        private System.Windows.Forms.RadioButton AccelRB;
        private System.Windows.Forms.RadioButton TurnRB;
        private System.Windows.Forms.RadioButton dpsAvgRB;
        private System.Windows.Forms.RadioButton dpsArmorRB;
        private System.Windows.Forms.Button UnitSortAcceptButton;
        private System.Windows.Forms.Button UnitSortCancelButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton SkPriceRB;
        private System.Windows.Forms.RadioButton SkBuildTimeRB;
        private System.Windows.Forms.RadioButton dpsShieldRB;
        private System.Windows.Forms.ComboBox DurabilityBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton InternalRB;
        private System.Windows.Forms.RadioButton ComplementRB;
        private System.Windows.Forms.ComboBox FighterTypeBox;
        private System.Windows.Forms.ComboBox ReserveBox;
        private System.Windows.Forms.CheckBox AccuracyCheckBox;
        private System.Windows.Forms.RadioButton GarrisonValueRB;
        private System.Windows.Forms.RadioButton GarrisonSlotsRB;
        private System.Windows.Forms.RadioButton ShipyardRB;
        private System.Windows.Forms.RadioButton CrewRB;
        private System.Windows.Forms.CheckBox ComplementCheckBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.RadioButton NameCountRB;
        private System.Windows.Forms.RadioButton radioButton1;
    }
}