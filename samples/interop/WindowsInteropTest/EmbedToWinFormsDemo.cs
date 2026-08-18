using System;
using System.Windows.Forms;
using ControlCatalog;
using AvaloniaButton = Avalonia.Controls.Button;
using AvaloniaStackPanel = Avalonia.Controls.StackPanel;
using AvaloniaTextBox = Avalonia.Controls.TextBox;
using AvaloniaWindow = Avalonia.Controls.Window;

namespace WindowsInteropTest
{
    public partial class EmbedToWinFormsDemo : Form
    {
        public EmbedToWinFormsDemo()
        {
            InitializeComponent();
            avaloniaHost.Content = new MainView();
        }

        private void OpenWindowButton_Click(object sender, EventArgs e)
        {
            var window = new AvaloniaWindow
            {
                Width = 300,
                Height = 300,
                Content = new AvaloniaStackPanel
                {
                    Children =
                    {
                        new AvaloniaButton { Content = "Button" },
                        new AvaloniaTextBox { Text = "Text" }
                    }
                }
            };
            window.Show();
        }
    }
}
