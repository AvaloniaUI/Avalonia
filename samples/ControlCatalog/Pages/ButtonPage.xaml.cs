using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class ButtonPage : UserControl
    {
        public ButtonPage()
        {
            this.InitializeComponent();
            DataContext = this;

            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(5000);
                Close();
            });
            

            this.FindControl<Button>("TestButton").Click += (sender, e)=>{
                Close();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Close ()
        {
            Application.Current.MainWindow.Close();
        }
    }
}
