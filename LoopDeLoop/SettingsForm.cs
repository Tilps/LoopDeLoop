using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LoopDeLoop
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            this.tabControl1.Region =
              new Region(
              new RectangleF(
              this.tabPage1.Left,
              this.tabPage1.Top,
              this.tabPage1.Width,
              this.tabPage1.Height)
              );
            treeView1.SelectedNode = treeView1.Nodes[0];
        }

        public bool SimpleSolver
        {
            get
            {
                return radioIterative.Checked;
            }
            set
            {
                if (value)
                    radioIterative.Checked = true;
                else
                    radioRecursive.Checked = true;
            }
        }

        public int SimpleSolverDepth
        {
            get
            {
                if (checkLookaheadRestrict.Checked)
                {
                    int res;
                    if (int.TryParse(textLookahead.Text, out res))
                    {
                        return res;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                }
                else
                    return int.MaxValue;
            }
            set
            {
                if (value == int.MaxValue)
                {
                    checkLookaheadRestrict.Checked = false;
                    textLookahead.Text = string.Empty;
                }
                else
                {
                    checkLookaheadRestrict.Checked = true;
                    textLookahead.Text = value.ToString();
                }
                UpdateLookaheadEnabled();
            }
        }

        public bool SimpleSolverNestedGuess
        {
            get
            {
                return checkNestedGuess.Checked;
            }
            set
            {
                checkNestedGuess.Checked = value;
            }
        }

        public int AutoMove
        {
            get
            {
                int res;
                if (int.TryParse(textAutoMove.Text, out res))
                    return res;
                else
                    return 0;
            }
            set
            {
                textAutoMove.Text = value.ToString();
            }
        }

        public bool AllowInvalidMoves
        {
            get
            {
                return checkAllowInvalid.Checked;
            }
            set
            {
                checkAllowInvalid.Checked = value;
            }
        }

        public bool AllowMultipleLoops
        {
            get
            {
                return checkAllowMultiLoop.Checked;
            }
            set
            {
                checkAllowMultiLoop.Checked = value;
            }
        }

        public bool ConsiderMultipleLoopsInAuto
        {
            get
            {
                return checkAutoConsider.Checked;
            }
            set
            {
                checkAutoConsider.Checked = value;
            }
        }

        public bool UseVisualSolver
        {
            get
            {
                return checkVisualSolver.Checked;
            }
            set
            {
                checkVisualSolver.Checked = value;
            }
        }

        public bool ShowColors
        {
            get
            {
                return checkShowColors.Checked;
            }
            set
            {
                checkShowColors.Checked = value;
            }
        }

        public bool ShowCellColors
        {
            get
            {
                return checkCellColors.Checked;
            }
            set
            {
                checkCellColors.Checked = value;
            }
        }

        public bool ShowCellColorsAdvanced
        {
            get
            {
                return checkCellColorsAdvanced.Checked;
            }
            set
            {
                checkCellColorsAdvanced.Checked = value;
            }
        }

        public bool UseICinSolver
        {
            get
            {
                return checkUseICinSolver.Checked;
            }
            set
            {
                checkUseICinSolver.Checked = value;
            }
        }

        public bool UseICinAutoMove
        {
            get
            {
                return checkUseICinAutoMove.Checked;
            }
            set
            {
                checkUseICinAutoMove.Checked = value;
            }
        }

        public bool AutoStart
        {
            get
            {
                return checkAutoStart.Checked;
            }
            set
            {
                checkAutoStart.Checked = value;
            }
        }

        public bool UseColoringInSolver
        {
            get
            {
                return checkColoringSolver.Checked;
            }
            set
            {
                checkColoringSolver.Checked = value;
            }
        }

        public bool UseDerivedColoringInSolver
        {
            get
            {
                return checkDerivedColoringSolver.Checked;
            }
            set
            {
                checkDerivedColoringSolver.Checked = value;
            }
        }

        public bool UseMergingInSolver
        {
            get
            {
                return checkMerging.Checked;
            }
            set
            {
                checkMerging.Checked = value;
            }
        }

        public bool UseCellColoringInSolver
        {
            get
            {
                return checkCellColoringSolver.Checked;
            }
            set
            {
                checkCellColoringSolver.Checked = value;
            }
        }

        public bool UseEdgeRestrictsInSolver
        {
            get
            {
                return checkEdgeRestrictsSolver.Checked;
            }
            set
            {
                checkEdgeRestrictsSolver.Checked = value;
            }
        }

        public bool UseColoringInAuto
        {
            get
            {
                return checkColoringAuto.Checked;
            }
            set
            {
                checkColoringAuto.Checked = value;
            }
        }

        public bool UseCellColoringInAuto
        {
            get
            {
                return checkAutoCellColoring.Checked;
            }
            set
            {
                checkAutoCellColoring.Checked = value;
            }
        }

        public bool UseCellPairsInAuto
        {
            get
            {
                return checkAutoCellPairs.Checked;
            }
            set
            {
                checkAutoCellPairs.Checked = value;
            }
        }

        public bool UseCellPairsInSolver
        {
            get
            {
                return checkSolverCellPairs.Enabled && checkSolverCellPairs.Checked;
            }
            set
            {
                checkSolverCellPairs.Checked = value;
                if (value)
                    checkSolverCellPairsTopLevel.Checked = true;
            }
        }

        public bool UseCellPairsTopLevelInSolver
        {
            get
            {
                return checkSolverCellPairsTopLevel.Checked;
            }
            set
            {
                checkSolverCellPairsTopLevel.Checked = value;
            }
        }

        public bool UseEdgeRestrictsInAuto
        {
            get
            {
                return checkEdgeRestrictsAuto.Checked;
            }
            set
            {
                checkEdgeRestrictsAuto.Checked = value;
            }
        }

        public bool ShowClock
        {
            get
            {
                return checkShowClock.Checked;
            }
            set
            {
                checkShowClock.Checked = value;
            }
        }

        public Font BoardFont
        {
            get
            {
                return boardFont;
            }
            set
            {
                boardFont = value;
            }
        }
        private Font boardFont;

        public double LineLengthFraction
        {
            get
            {
                double res;
                if (double.TryParse(textLengthFraction.Text, out res))
                {
                    if (res > 0.95) res = 0.95;
                    if (res < 0.05) res = 0.05;
                    return res;
                }
                else
                    return 0.5;
            }
            set
            {
                textLengthFraction.Text = value.ToString();
            }
        }

        public double BoringFraction
        {
            get
            {
                double res;
                if (double.TryParse(textBoringFraction.Text, out res))
                {
                    if (res > 0.95) res = 0.95;
                    if (res < 0.00) res = 0.00;
                    return res;
                }
                else
                    return 0.01;
            }
            set
            {
                textBoringFraction.Text = value.ToString();
            }
        }

        public string RatingCodes
        {
            get
            {
                return textRatingCodes.Text;
            }
            set
            {
                textRatingCodes.Text = value;
            }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {

        }

        private void radioIterative_CheckedChanged(object sender, EventArgs e)
        {
            if (radioIterative.Checked)
            {
                checkLookaheadRestrict.Enabled = true;
                checkNestedGuess.Enabled = true;
            }
            else
            {
                checkLookaheadRestrict.Enabled = false;
                checkNestedGuess.Enabled = false;
            }
            UpdateLookaheadEnabled();
        }

        private void UpdateLookaheadEnabled()
        {
            if (radioIterative.Checked && checkLookaheadRestrict.Checked)
                textLookahead.Enabled = true;
            else
                textLookahead.Enabled = false;
        }

        private void checkLookaheadRestrict_CheckedChanged(object sender, EventArgs e)
        {
            UpdateLookaheadEnabled();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {

        }

        private void buttonChangeBoardFont_Click(object sender, EventArgs e)
        {
            FontDialog dialog = new FontDialog();
            dialog.Font = boardFont;
            if (dialog.ShowDialog() == DialogResult.OK)
                boardFont = dialog.Font;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (e.Node.Name == tabControl1.TabPages[i].Text)
                {
                    tabControl1.SelectedIndex = i;
                }
            }

        }

        private void checkCellColors_CheckedChanged(object sender, EventArgs e)
        {
            checkCellColorsAdvanced.Enabled = checkCellColors.Checked;
        }

        private void checkColoringSolver_CheckedChanged(object sender, EventArgs e)
        {
            checkDerivedColoringSolver.Enabled = checkColoringSolver.Checked;
        }

        private void checkSolverCellPairsTopLevel_CheckedChanged(object sender, EventArgs e)
        {
            checkSolverCellPairs.Enabled = checkSolverCellPairsTopLevel.Checked;
        }
    }
}