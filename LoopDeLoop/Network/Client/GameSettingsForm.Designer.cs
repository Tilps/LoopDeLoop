namespace LoopDeLoop.Network.Client
{
    partial class GameSettingsForm
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
            this.radioIterative = new System.Windows.Forms.RadioButton();
            this.radioRecursive = new System.Windows.Forms.RadioButton();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkLookaheadRestrict = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textLookahead = new System.Windows.Forms.TextBox();
            this.checkAllowMultiLoop = new System.Windows.Forms.CheckBox();
            this.checkUseICinSolver = new System.Windows.Forms.CheckBox();
            this.comboMeshType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textSize = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // radioIterative
            // 
            this.radioIterative.AutoSize = true;
            this.radioIterative.Location = new System.Drawing.Point(12, 39);
            this.radioIterative.Name = "radioIterative";
            this.radioIterative.Size = new System.Drawing.Size(356, 17);
            this.radioIterative.TabIndex = 0;
            this.radioIterative.TabStop = true;
            this.radioIterative.Text = "Simplified Solver (generates easier puzzles but can\'t solve all puzzles)";
            this.radioIterative.UseVisualStyleBackColor = true;
            this.radioIterative.CheckedChanged += new System.EventHandler(this.radioIterative_CheckedChanged);
            // 
            // radioRecursive
            // 
            this.radioRecursive.AutoSize = true;
            this.radioRecursive.Location = new System.Drawing.Point(12, 100);
            this.radioRecursive.Name = "radioRecursive";
            this.radioRecursive.Size = new System.Drawing.Size(323, 17);
            this.radioRecursive.TabIndex = 1;
            this.radioRecursive.TabStop = true;
            this.radioRecursive.Text = "Full Solver (can solve anything, but generates harder puzzles)";
            this.radioRecursive.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(370, 177);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(451, 177);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkLookaheadRestrict
            // 
            this.checkLookaheadRestrict.AutoSize = true;
            this.checkLookaheadRestrict.Location = new System.Drawing.Point(29, 62);
            this.checkLookaheadRestrict.Name = "checkLookaheadRestrict";
            this.checkLookaheadRestrict.Size = new System.Drawing.Size(244, 17);
            this.checkLookaheadRestrict.TabIndex = 4;
            this.checkLookaheadRestrict.Text = "Restrict Simple Solver forced move lookahead";
            this.checkLookaheadRestrict.UseVisualStyleBackColor = true;
            this.checkLookaheadRestrict.CheckedChanged += new System.EventHandler(this.checkLookaheadRestrict_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(51, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Lookahead";
            // 
            // textLookahead
            // 
            this.textLookahead.Location = new System.Drawing.Point(118, 81);
            this.textLookahead.Name = "textLookahead";
            this.textLookahead.Size = new System.Drawing.Size(76, 21);
            this.textLookahead.TabIndex = 6;
            // 
            // checkAllowMultiLoop
            // 
            this.checkAllowMultiLoop.AutoSize = true;
            this.checkAllowMultiLoop.Location = new System.Drawing.Point(12, 146);
            this.checkAllowMultiLoop.Name = "checkAllowMultiLoop";
            this.checkAllowMultiLoop.Size = new System.Drawing.Size(249, 17);
            this.checkAllowMultiLoop.TabIndex = 10;
            this.checkAllowMultiLoop.Text = "Consider multiple loops as alternative solutions";
            this.checkAllowMultiLoop.UseVisualStyleBackColor = true;
            // 
            // checkUseICinSolver
            // 
            this.checkUseICinSolver.AutoSize = true;
            this.checkUseICinSolver.Location = new System.Drawing.Point(12, 123);
            this.checkUseICinSolver.Name = "checkUseICinSolver";
            this.checkUseICinSolver.Size = new System.Drawing.Size(527, 17);
            this.checkUseICinSolver.TabIndex = 12;
            this.checkUseICinSolver.Text = "Allow Solver to consider cell intersection interactions as simple (generates hard" +
                "er puzzles in simple mode)";
            this.checkUseICinSolver.UseVisualStyleBackColor = true;
            // 
            // comboMeshType
            // 
            this.comboMeshType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMeshType.FormattingEnabled = true;
            this.comboMeshType.Items.AddRange(new object[] {
            "Square",
            "Triangle",
            "Hexagon",
            "Octagon",
            "Hexagon2",
            "Square2",
            "Pentagon",
            "Hexagon3"});
            this.comboMeshType.Location = new System.Drawing.Point(12, 12);
            this.comboMeshType.Name = "comboMeshType";
            this.comboMeshType.Size = new System.Drawing.Size(92, 21);
            this.comboMeshType.TabIndex = 15;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(110, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Size:";
            // 
            // textSize
            // 
            this.textSize.Location = new System.Drawing.Point(146, 12);
            this.textSize.Name = "textSize";
            this.textSize.Size = new System.Drawing.Size(66, 21);
            this.textSize.TabIndex = 13;
            // 
            // GameSettingsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(538, 212);
            this.Controls.Add(this.comboMeshType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textSize);
            this.Controls.Add(this.checkUseICinSolver);
            this.Controls.Add(this.checkAllowMultiLoop);
            this.Controls.Add(this.textLookahead);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkLookaheadRestrict);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.radioRecursive);
            this.Controls.Add(this.radioIterative);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "GameSettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radioIterative;
        private System.Windows.Forms.RadioButton radioRecursive;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkLookaheadRestrict;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textLookahead;
        private System.Windows.Forms.CheckBox checkAllowMultiLoop;
        private System.Windows.Forms.CheckBox checkUseICinSolver;
        private System.Windows.Forms.ComboBox comboMeshType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textSize;
    }
}