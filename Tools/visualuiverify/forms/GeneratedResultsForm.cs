using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisualUIAVerify.Forms
{
    public partial class GeneratedResultsForm : Form
    {
        public GeneratedResultsForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void LoadClipboard()
        {
            Clipboard.SetText(codeTextBox.Text);
        }
    }
}
