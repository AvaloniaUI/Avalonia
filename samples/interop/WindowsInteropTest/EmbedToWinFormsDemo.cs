using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Controls;
using ControlCatalog;

namespace WindowsInteropTest
{
    public partial class EmbedToWinFormsDemo : Form
    {
        public EmbedToWinFormsDemo()
        {
            InitializeComponent();
            var ctrl = new EmbeddableControl();
            ctrl.Content = new ControlCatalogControl();
            ctrl.ApplyTemplate();
            avaloniaHost.Child = ctrl;
        }
    }
}
