using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsInteropTest
{
    public partial class SelectorForm : Form
    {
        public SelectorForm()
        {
            InitializeComponent();
        }

        private void btnEmbedToWinForms_Click(object sender, EventArgs e)
        {
            new EmbedToWinFormsDemo().ShowDialog(this);
        }

        private void btnEmbedToWpf_Click(object sender, EventArgs e)
        {
            new EmbedToWpfDemo().ShowDialog();
        }
    }
}
