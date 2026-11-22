using System;
using System.Windows.Forms;

namespace LoopDeLoop
{
    public partial class HelpForm : Form
    {
        public HelpForm()
        {
            InitializeComponent();
        }

        private void HelpForm_Load(object sender, EventArgs e)
        {
            textHelp.Select(0, 0);
        }
    }
}
