namespace LoopDeLoop
{
    partial class SettingsForm
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Puzzle Creation");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Appearance");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Play");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Puzzle Solver");
            this.radioIterative = new System.Windows.Forms.RadioButton();
            this.radioRecursive = new System.Windows.Forms.RadioButton();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkLookaheadRestrict = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textLookahead = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textAutoMove = new System.Windows.Forms.TextBox();
            this.checkAllowInvalid = new System.Windows.Forms.CheckBox();
            this.checkAllowMultiLoop = new System.Windows.Forms.CheckBox();
            this.checkAutoStart = new System.Windows.Forms.CheckBox();
            this.checkUseICinSolver = new System.Windows.Forms.CheckBox();
            this.checkUseICinAutoMove = new System.Windows.Forms.CheckBox();
            this.buttonChangeBoardFont = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.checkVisualSolver = new System.Windows.Forms.CheckBox();
            this.checkShowColors = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoringFraction = new System.Windows.Forms.TextBox();
            this.checkEdgeRestrictsSolver = new System.Windows.Forms.CheckBox();
            this.checkMerging = new System.Windows.Forms.CheckBox();
            this.checkDerivedColoringSolver = new System.Windows.Forms.CheckBox();
            this.checkCellColoringSolver = new System.Windows.Forms.CheckBox();
            this.checkNestedGuess = new System.Windows.Forms.CheckBox();
            this.checkColoringSolver = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textRatingCodes = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textLengthFraction = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.checkCellColorsAdvanced = new System.Windows.Forms.CheckBox();
            this.checkCellColors = new System.Windows.Forms.CheckBox();
            this.checkShowClock = new System.Windows.Forms.CheckBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.checkEdgeRestrictsAuto = new System.Windows.Forms.CheckBox();
            this.checkAutoCellColoring = new System.Windows.Forms.CheckBox();
            this.checkColoringAuto = new System.Windows.Forms.CheckBox();
            this.checkAutoConsider = new System.Windows.Forms.CheckBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.checkSolverCellPairs = new System.Windows.Forms.CheckBox();
            this.checkSolverCellPairsTopLevel = new System.Windows.Forms.CheckBox();
            this.checkAutoCellPairs = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioIterative
            // 
            this.radioIterative.AutoSize = true;
            this.radioIterative.Location = new System.Drawing.Point(6, 6);
            this.radioIterative.Name = "radioIterative";
            this.radioIterative.Size = new System.Drawing.Size(107, 17);
            this.radioIterative.TabIndex = 0;
            this.radioIterative.TabStop = true;
            this.radioIterative.Text = "Simple Generator";
            this.radioIterative.UseVisualStyleBackColor = true;
            this.radioIterative.CheckedChanged += new System.EventHandler(this.radioIterative_CheckedChanged);
            // 
            // radioRecursive
            // 
            this.radioRecursive.AutoSize = true;
            this.radioRecursive.Location = new System.Drawing.Point(6, 96);
            this.radioRecursive.Name = "radioRecursive";
            this.radioRecursive.Size = new System.Drawing.Size(243, 17);
            this.radioRecursive.TabIndex = 1;
            this.radioRecursive.TabStop = true;
            this.radioRecursive.Text = "Full Generator (Can make Very hard puzzles!)";
            this.radioRecursive.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(556, 424);
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
            this.buttonCancel.Location = new System.Drawing.Point(637, 424);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkLookaheadRestrict
            // 
            this.checkLookaheadRestrict.AutoSize = true;
            this.checkLookaheadRestrict.Location = new System.Drawing.Point(23, 29);
            this.checkLookaheadRestrict.Name = "checkLookaheadRestrict";
            this.checkLookaheadRestrict.Size = new System.Drawing.Size(200, 17);
            this.checkLookaheadRestrict.TabIndex = 4;
            this.checkLookaheadRestrict.Text = "Restrict Simple Generator lookahead";
            this.checkLookaheadRestrict.UseVisualStyleBackColor = true;
            this.checkLookaheadRestrict.CheckedChanged += new System.EventHandler(this.checkLookaheadRestrict_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(45, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Lookahead";
            // 
            // textLookahead
            // 
            this.textLookahead.Location = new System.Drawing.Point(110, 46);
            this.textLookahead.Name = "textLookahead";
            this.textLookahead.Size = new System.Drawing.Size(76, 21);
            this.textLookahead.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "AutoMove Lookahead";
            // 
            // textAutoMove
            // 
            this.textAutoMove.Location = new System.Drawing.Point(119, 26);
            this.textAutoMove.Name = "textAutoMove";
            this.textAutoMove.Size = new System.Drawing.Size(76, 21);
            this.textAutoMove.TabIndex = 8;
            // 
            // checkAllowInvalid
            // 
            this.checkAllowInvalid.AutoSize = true;
            this.checkAllowInvalid.Location = new System.Drawing.Point(3, 99);
            this.checkAllowInvalid.Name = "checkAllowInvalid";
            this.checkAllowInvalid.Size = new System.Drawing.Size(170, 17);
            this.checkAllowInvalid.TabIndex = 9;
            this.checkAllowInvalid.Text = "Allow \'obviously\' invalid moves";
            this.checkAllowInvalid.UseVisualStyleBackColor = true;
            // 
            // checkAllowMultiLoop
            // 
            this.checkAllowMultiLoop.AutoSize = true;
            this.checkAllowMultiLoop.Location = new System.Drawing.Point(6, 142);
            this.checkAllowMultiLoop.Name = "checkAllowMultiLoop";
            this.checkAllowMultiLoop.Size = new System.Drawing.Size(365, 17);
            this.checkAllowMultiLoop.TabIndex = 10;
            this.checkAllowMultiLoop.Text = "Generate puzzles with no multiple loop solutions (Makes puzzles easier)";
            this.checkAllowMultiLoop.UseVisualStyleBackColor = true;
            // 
            // checkAutoStart
            // 
            this.checkAutoStart.AutoSize = true;
            this.checkAutoStart.Location = new System.Drawing.Point(3, 3);
            this.checkAutoStart.Name = "checkAutoStart";
            this.checkAutoStart.Size = new System.Drawing.Size(73, 17);
            this.checkAutoStart.TabIndex = 11;
            this.checkAutoStart.Text = "AutoStart";
            this.checkAutoStart.UseVisualStyleBackColor = true;
            // 
            // checkUseICinSolver
            // 
            this.checkUseICinSolver.AutoSize = true;
            this.checkUseICinSolver.Location = new System.Drawing.Point(6, 119);
            this.checkUseICinSolver.Name = "checkUseICinSolver";
            this.checkUseICinSolver.Size = new System.Drawing.Size(318, 17);
            this.checkUseICinSolver.TabIndex = 12;
            this.checkUseICinSolver.Text = "Consider cell intersection interactions (Makes puzzles harder)";
            this.checkUseICinSolver.UseVisualStyleBackColor = true;
            // 
            // checkUseICinAutoMove
            // 
            this.checkUseICinAutoMove.AutoSize = true;
            this.checkUseICinAutoMove.Location = new System.Drawing.Point(3, 53);
            this.checkUseICinAutoMove.Name = "checkUseICinAutoMove";
            this.checkUseICinAutoMove.Size = new System.Drawing.Size(249, 17);
            this.checkUseICinAutoMove.TabIndex = 13;
            this.checkUseICinAutoMove.Text = "Let AutoMove use cell intersection interactions";
            this.checkUseICinAutoMove.UseVisualStyleBackColor = true;
            // 
            // buttonChangeBoardFont
            // 
            this.buttonChangeBoardFont.Location = new System.Drawing.Point(72, 3);
            this.buttonChangeBoardFont.Name = "buttonChangeBoardFont";
            this.buttonChangeBoardFont.Size = new System.Drawing.Size(75, 23);
            this.buttonChangeBoardFont.TabIndex = 14;
            this.buttonChangeBoardFont.Text = "Change";
            this.buttonChangeBoardFont.UseVisualStyleBackColor = true;
            this.buttonChangeBoardFont.Click += new System.EventHandler(this.buttonChangeBoardFont_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "Board Font";
            // 
            // checkVisualSolver
            // 
            this.checkVisualSolver.AutoSize = true;
            this.checkVisualSolver.Location = new System.Drawing.Point(3, 3);
            this.checkVisualSolver.Name = "checkVisualSolver";
            this.checkVisualSolver.Size = new System.Drawing.Size(107, 17);
            this.checkVisualSolver.TabIndex = 16;
            this.checkVisualSolver.Text = "Use Visual Solver";
            this.checkVisualSolver.UseVisualStyleBackColor = true;
            // 
            // checkShowColors
            // 
            this.checkShowColors.AutoSize = true;
            this.checkShowColors.Location = new System.Drawing.Point(10, 32);
            this.checkShowColors.Name = "checkShowColors";
            this.checkShowColors.Size = new System.Drawing.Size(137, 17);
            this.checkShowColors.TabIndex = 17;
            this.checkShowColors.Text = "Use Empty Edge Colors";
            this.checkShowColors.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(149, -10);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(563, 432);
            this.tabControl1.TabIndex = 18;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.checkSolverCellPairs);
            this.tabPage1.Controls.Add(this.checkSolverCellPairsTopLevel);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.textBoringFraction);
            this.tabPage1.Controls.Add(this.checkEdgeRestrictsSolver);
            this.tabPage1.Controls.Add(this.checkMerging);
            this.tabPage1.Controls.Add(this.checkDerivedColoringSolver);
            this.tabPage1.Controls.Add(this.checkCellColoringSolver);
            this.tabPage1.Controls.Add(this.checkNestedGuess);
            this.tabPage1.Controls.Add(this.checkColoringSolver);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.textRatingCodes);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.textLengthFraction);
            this.tabPage1.Controls.Add(this.checkLookaheadRestrict);
            this.tabPage1.Controls.Add(this.radioIterative);
            this.tabPage1.Controls.Add(this.radioRecursive);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.textLookahead);
            this.tabPage1.Controls.Add(this.checkAllowMultiLoop);
            this.tabPage1.Controls.Add(this.checkUseICinSolver);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(555, 406);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Puzzle Creation";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 359);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(132, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Target boring bits fraction";
            // 
            // textBoringFraction
            // 
            this.textBoringFraction.Location = new System.Drawing.Point(179, 356);
            this.textBoringFraction.Name = "textBoringFraction";
            this.textBoringFraction.Size = new System.Drawing.Size(76, 21);
            this.textBoringFraction.TabIndex = 24;
            // 
            // checkEdgeRestrictsSolver
            // 
            this.checkEdgeRestrictsSolver.AutoSize = true;
            this.checkEdgeRestrictsSolver.Location = new System.Drawing.Point(6, 257);
            this.checkEdgeRestrictsSolver.Name = "checkEdgeRestrictsSolver";
            this.checkEdgeRestrictsSolver.Size = new System.Drawing.Size(241, 17);
            this.checkEdgeRestrictsSolver.TabIndex = 22;
            this.checkEdgeRestrictsSolver.Text = "Use edge restrictions (Makes puzzles harder)";
            this.checkEdgeRestrictsSolver.UseVisualStyleBackColor = true;
            // 
            // checkMerging
            // 
            this.checkMerging.AutoSize = true;
            this.checkMerging.Location = new System.Drawing.Point(6, 234);
            this.checkMerging.Name = "checkMerging";
            this.checkMerging.Size = new System.Drawing.Size(271, 17);
            this.checkMerging.TabIndex = 21;
            this.checkMerging.Text = "Use common consequences (Makes puzzles harder)";
            this.checkMerging.UseVisualStyleBackColor = true;
            // 
            // checkDerivedColoringSolver
            // 
            this.checkDerivedColoringSolver.AutoSize = true;
            this.checkDerivedColoringSolver.Enabled = false;
            this.checkDerivedColoringSolver.Location = new System.Drawing.Point(23, 188);
            this.checkDerivedColoringSolver.Name = "checkDerivedColoringSolver";
            this.checkDerivedColoringSolver.Size = new System.Drawing.Size(353, 17);
            this.checkDerivedColoringSolver.TabIndex = 20;
            this.checkDerivedColoringSolver.Text = "Derive Coloring from opposite consequences (Makes puzzles harder)";
            this.checkDerivedColoringSolver.UseVisualStyleBackColor = true;
            // 
            // checkCellColoringSolver
            // 
            this.checkCellColoringSolver.AutoSize = true;
            this.checkCellColoringSolver.Location = new System.Drawing.Point(6, 211);
            this.checkCellColoringSolver.Name = "checkCellColoringSolver";
            this.checkCellColoringSolver.Size = new System.Drawing.Size(273, 17);
            this.checkCellColoringSolver.TabIndex = 19;
            this.checkCellColoringSolver.Text = "Use Inside/Outside Coloring (Makes puzzles harder)";
            this.checkCellColoringSolver.UseVisualStyleBackColor = true;
            // 
            // checkNestedGuess
            // 
            this.checkNestedGuess.AutoSize = true;
            this.checkNestedGuess.Location = new System.Drawing.Point(23, 73);
            this.checkNestedGuess.Name = "checkNestedGuess";
            this.checkNestedGuess.Size = new System.Drawing.Size(262, 17);
            this.checkNestedGuess.TabIndex = 18;
            this.checkNestedGuess.Text = "Allow Nested Guess (Makes puzzles much harder)";
            this.checkNestedGuess.UseVisualStyleBackColor = true;
            // 
            // checkColoringSolver
            // 
            this.checkColoringSolver.AutoSize = true;
            this.checkColoringSolver.Location = new System.Drawing.Point(6, 165);
            this.checkColoringSolver.Name = "checkColoringSolver";
            this.checkColoringSolver.Size = new System.Drawing.Size(369, 17);
            this.checkColoringSolver.TabIndex = 17;
            this.checkColoringSolver.Text = "Use Coloring (Makes puzzles harder, even more so with interactions on)";
            this.checkColoringSolver.UseVisualStyleBackColor = true;
            this.checkColoringSolver.CheckedChanged += new System.EventHandler(this.checkColoringSolver_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 386);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(109, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "Rating types to show";
            // 
            // textRatingCodes
            // 
            this.textRatingCodes.Location = new System.Drawing.Point(179, 383);
            this.textRatingCodes.Name = "textRatingCodes";
            this.textRatingCodes.Size = new System.Drawing.Size(76, 21);
            this.textRatingCodes.TabIndex = 16;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 332);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(167, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Target line length fraction of max";
            // 
            // textLengthFraction
            // 
            this.textLengthFraction.Location = new System.Drawing.Point(179, 329);
            this.textLengthFraction.Name = "textLengthFraction";
            this.textLengthFraction.Size = new System.Drawing.Size(76, 21);
            this.textLengthFraction.TabIndex = 14;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.checkCellColorsAdvanced);
            this.tabPage2.Controls.Add(this.checkCellColors);
            this.tabPage2.Controls.Add(this.checkShowClock);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.checkShowColors);
            this.tabPage2.Controls.Add(this.buttonChangeBoardFont);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(555, 360);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Appearance";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // checkCellColorsAdvanced
            // 
            this.checkCellColorsAdvanced.AutoSize = true;
            this.checkCellColorsAdvanced.Enabled = false;
            this.checkCellColorsAdvanced.Location = new System.Drawing.Point(22, 101);
            this.checkCellColorsAdvanced.Name = "checkCellColorsAdvanced";
            this.checkCellColorsAdvanced.Size = new System.Drawing.Size(217, 17);
            this.checkCellColorsAdvanced.TabIndex = 20;
            this.checkCellColorsAdvanced.Text = "Show Inside/Outside Colors (Advanced)";
            this.checkCellColorsAdvanced.UseVisualStyleBackColor = true;
            // 
            // checkCellColors
            // 
            this.checkCellColors.AutoSize = true;
            this.checkCellColors.Location = new System.Drawing.Point(9, 78);
            this.checkCellColors.Name = "checkCellColors";
            this.checkCellColors.Size = new System.Drawing.Size(158, 17);
            this.checkCellColors.TabIndex = 19;
            this.checkCellColors.Text = "Show Inside/Outside Colors";
            this.checkCellColors.UseVisualStyleBackColor = true;
            this.checkCellColors.CheckedChanged += new System.EventHandler(this.checkCellColors_CheckedChanged);
            // 
            // checkShowClock
            // 
            this.checkShowClock.AutoSize = true;
            this.checkShowClock.Location = new System.Drawing.Point(9, 55);
            this.checkShowClock.Name = "checkShowClock";
            this.checkShowClock.Size = new System.Drawing.Size(130, 17);
            this.checkShowClock.TabIndex = 18;
            this.checkShowClock.Text = "Show game time clock";
            this.checkShowClock.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.checkAutoCellPairs);
            this.tabPage3.Controls.Add(this.checkEdgeRestrictsAuto);
            this.tabPage3.Controls.Add(this.checkAutoCellColoring);
            this.tabPage3.Controls.Add(this.checkColoringAuto);
            this.tabPage3.Controls.Add(this.checkAutoConsider);
            this.tabPage3.Controls.Add(this.checkAutoStart);
            this.tabPage3.Controls.Add(this.label2);
            this.tabPage3.Controls.Add(this.checkUseICinAutoMove);
            this.tabPage3.Controls.Add(this.textAutoMove);
            this.tabPage3.Controls.Add(this.checkAllowInvalid);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(555, 406);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Play";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkEdgeRestrictsAuto
            // 
            this.checkEdgeRestrictsAuto.AutoSize = true;
            this.checkEdgeRestrictsAuto.Location = new System.Drawing.Point(5, 168);
            this.checkEdgeRestrictsAuto.Name = "checkEdgeRestrictsAuto";
            this.checkEdgeRestrictsAuto.Size = new System.Drawing.Size(196, 17);
            this.checkEdgeRestrictsAuto.TabIndex = 17;
            this.checkEdgeRestrictsAuto.Text = "Let AutoMove use edge restrictions";
            this.checkEdgeRestrictsAuto.UseVisualStyleBackColor = true;
            // 
            // checkAutoCellColoring
            // 
            this.checkAutoCellColoring.AutoSize = true;
            this.checkAutoCellColoring.Location = new System.Drawing.Point(5, 145);
            this.checkAutoCellColoring.Name = "checkAutoCellColoring";
            this.checkAutoCellColoring.Size = new System.Drawing.Size(228, 17);
            this.checkAutoCellColoring.TabIndex = 16;
            this.checkAutoCellColoring.Text = "Let AutoMove use Inside/Outside Coloring";
            this.checkAutoCellColoring.UseVisualStyleBackColor = true;
            // 
            // checkColoringAuto
            // 
            this.checkColoringAuto.AutoSize = true;
            this.checkColoringAuto.Location = new System.Drawing.Point(3, 122);
            this.checkColoringAuto.Name = "checkColoringAuto";
            this.checkColoringAuto.Size = new System.Drawing.Size(155, 17);
            this.checkColoringAuto.TabIndex = 15;
            this.checkColoringAuto.Text = "Let AutoMove use Coloring";
            this.checkColoringAuto.UseVisualStyleBackColor = true;
            // 
            // checkAutoConsider
            // 
            this.checkAutoConsider.AutoSize = true;
            this.checkAutoConsider.Location = new System.Drawing.Point(3, 76);
            this.checkAutoConsider.Name = "checkAutoConsider";
            this.checkAutoConsider.Size = new System.Drawing.Size(245, 17);
            this.checkAutoConsider.TabIndex = 14;
            this.checkAutoConsider.Text = "Let AutoMove avoid closing the loop too early";
            this.checkAutoConsider.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.checkVisualSolver);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(555, 360);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Puzzle Solver";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = "treeView1";
            treeNode1.Name = "Puzzle Creation";
            treeNode1.Text = "Puzzle Creation";
            treeNode2.Name = "Appearance";
            treeNode2.Text = "Appearance";
            treeNode3.Name = "Play";
            treeNode3.Text = "Play";
            treeNode4.Name = "Puzzle Solver";
            treeNode4.Text = "Puzzle Solver";
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3,
            treeNode4});
            this.treeView1.Size = new System.Drawing.Size(135, 408);
            this.treeView1.TabIndex = 19;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // checkSolverCellPairs
            // 
            this.checkSolverCellPairs.AutoSize = true;
            this.checkSolverCellPairs.Enabled = false;
            this.checkSolverCellPairs.Location = new System.Drawing.Point(23, 303);
            this.checkSolverCellPairs.Name = "checkSolverCellPairs";
            this.checkSolverCellPairs.Size = new System.Drawing.Size(323, 17);
            this.checkSolverCellPairs.TabIndex = 26;
            this.checkSolverCellPairs.Text = "Use Cell Pairs inside trials (Makes puzzles significantly harder.)";
            this.checkSolverCellPairs.UseVisualStyleBackColor = true;
            // 
            // checkSolverCellPairsTopLevel
            // 
            this.checkSolverCellPairsTopLevel.AutoSize = true;
            this.checkSolverCellPairsTopLevel.Location = new System.Drawing.Point(6, 280);
            this.checkSolverCellPairsTopLevel.Name = "checkSolverCellPairsTopLevel";
            this.checkSolverCellPairsTopLevel.Size = new System.Drawing.Size(353, 17);
            this.checkSolverCellPairsTopLevel.TabIndex = 25;
            this.checkSolverCellPairsTopLevel.Text = "Use Cell Pairs (Generates puzzles faster, sometimes slightly harder.)";
            this.checkSolverCellPairsTopLevel.UseVisualStyleBackColor = true;
            this.checkSolverCellPairsTopLevel.CheckedChanged += new System.EventHandler(this.checkSolverCellPairsTopLevel_CheckedChanged);
            // 
            // checkAutoCellPairs
            // 
            this.checkAutoCellPairs.AutoSize = true;
            this.checkAutoCellPairs.Location = new System.Drawing.Point(5, 191);
            this.checkAutoCellPairs.Name = "checkAutoCellPairs";
            this.checkAutoCellPairs.Size = new System.Drawing.Size(159, 17);
            this.checkAutoCellPairs.TabIndex = 18;
            this.checkAutoCellPairs.Text = "Let AutoMove use Cell Pairs";
            this.checkAutoCellPairs.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(724, 459);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton radioIterative;
        private System.Windows.Forms.RadioButton radioRecursive;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkLookaheadRestrict;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textLookahead;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textAutoMove;
        private System.Windows.Forms.CheckBox checkAllowInvalid;
        private System.Windows.Forms.CheckBox checkAllowMultiLoop;
        private System.Windows.Forms.CheckBox checkAutoStart;
        private System.Windows.Forms.CheckBox checkUseICinSolver;
        private System.Windows.Forms.CheckBox checkUseICinAutoMove;
        private System.Windows.Forms.Button buttonChangeBoardFont;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkVisualSolver;
        private System.Windows.Forms.CheckBox checkShowColors;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.CheckBox checkAutoConsider;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textLengthFraction;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textRatingCodes;
        private System.Windows.Forms.CheckBox checkColoringSolver;
        private System.Windows.Forms.CheckBox checkColoringAuto;
        private System.Windows.Forms.CheckBox checkShowClock;
        private System.Windows.Forms.CheckBox checkNestedGuess;
        private System.Windows.Forms.CheckBox checkCellColoringSolver;
        private System.Windows.Forms.CheckBox checkCellColorsAdvanced;
        private System.Windows.Forms.CheckBox checkCellColors;
        private System.Windows.Forms.CheckBox checkAutoCellColoring;
        private System.Windows.Forms.CheckBox checkDerivedColoringSolver;
        private System.Windows.Forms.CheckBox checkMerging;
        private System.Windows.Forms.CheckBox checkEdgeRestrictsSolver;
        private System.Windows.Forms.CheckBox checkEdgeRestrictsAuto;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoringFraction;
        private System.Windows.Forms.CheckBox checkSolverCellPairs;
        private System.Windows.Forms.CheckBox checkSolverCellPairsTopLevel;
        private System.Windows.Forms.CheckBox checkAutoCellPairs;
    }
}