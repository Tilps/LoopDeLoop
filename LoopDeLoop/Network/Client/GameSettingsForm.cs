using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LoopDeLoop.Network.Client
{
    public partial class GameSettingsForm : Form
    {
        public GameSettingsForm()
        {
            InitializeComponent();
        }

        public bool ReadOnly
        {
            get
            {
                return readOnly;
            }
            set
            {
                readOnly = value;
                comboMeshType.Enabled = !readOnly;
                textLookahead.Enabled = !readOnly;
                textSize.Enabled = !readOnly;
                buttonOK.Enabled = !readOnly;
                checkAllowMultiLoop.Enabled = !readOnly;
                checkLookaheadRestrict.Enabled = !readOnly;
                checkUseICinSolver.Enabled = !readOnly;
                radioIterative.Enabled = !readOnly;
                radioRecursive.Enabled = !readOnly;
            }
        }
        private bool readOnly;

        public MeshType MeshStyle
        {
            get
            {
                return LoopDeLoopForm.MeshTypeFromString(comboMeshType.SelectedItem.ToString());
            }
            set
            {
                comboMeshType.SelectedItem = LoopDeLoopForm.StringFromMeshType(value);
            }
        }

        public string SizeText
        {
            get
            {
                return textSize.Text;
            }
            set
            {
                textSize.Text = value;
            }
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

        private void buttonOK_Click(object sender, EventArgs e)
        {

        }

        private void radioIterative_CheckedChanged(object sender, EventArgs e)
        {
            if (radioIterative.Checked && !readOnly)
            {
                checkLookaheadRestrict.Enabled = true;
            }
            else
            {
                checkLookaheadRestrict.Enabled = false;
            }
            UpdateLookaheadEnabled();
        }

        private void UpdateLookaheadEnabled()
        {
            if (radioIterative.Checked && checkLookaheadRestrict.Checked && !readOnly)
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
    }
}