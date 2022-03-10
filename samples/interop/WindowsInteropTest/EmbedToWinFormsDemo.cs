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
using Avalonia.Rendering;
using Avalonia.VisualTree;
using ControlCatalog;

namespace WindowsInteropTest
{
    public partial class EmbedToWinFormsDemo : Form
    {
        private readonly IRenderer _renderer;

        public EmbedToWinFormsDemo()
        {
            InitializeComponent();
            avaloniaHost.Content = new MainView();
            _renderer = ((TopLevel)avaloniaHost.Content.GetVisualRoot()).Renderer;
            _renderer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _renderer.Stop();
            base.OnClosed(e);
        }
    }
}
