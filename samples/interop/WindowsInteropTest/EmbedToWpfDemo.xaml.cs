using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using ControlCatalog;
using Window = System.Windows.Window;

namespace WindowsInteropTest
{
    /// <summary>
    /// Interaction logic for EmbedToWpfDemo.xaml
    /// </summary>
    public partial class EmbedToWpfDemo : Window
    {
        private IRenderer _renderer;
        public EmbedToWpfDemo()
        {
            InitializeComponent();
            var view = new MainView();
            Host.Content = view;
            var tl = (TopLevel)view.GetVisualRoot();
            tl.AttachDevTools();
            _renderer = tl.Renderer;
            _renderer.Start();
            var btn = (Avalonia.Controls.Button) RightBtn.Content;
            btn.Click += delegate
            {
                btn.Content += "!";
            };

        }

        protected override void OnClosed(EventArgs e)
        {
            _renderer.Stop();
            base.OnClosed(e);
        }
    }
}
