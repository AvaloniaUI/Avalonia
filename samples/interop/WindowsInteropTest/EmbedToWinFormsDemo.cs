using System.Windows.Forms;
using ControlCatalog;

namespace WindowsInteropTest
{
    public partial class EmbedToWinFormsDemo : Form
    {
        public EmbedToWinFormsDemo()
        {
            InitializeComponent();
            avaloniaHost.Content = new MainView();
        }
    }
}
