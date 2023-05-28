using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JSONExplorerPro
{
    public partial class InputDialog : Form
    {
        public string InputValue { get; private set; }

        public InputDialog(string caption, string label)
        {
            InitializeComponent();

            lblCaption.Text = caption;
            lblLabel.Text = label;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            InputValue = txtInput.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
