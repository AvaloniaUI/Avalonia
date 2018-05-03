using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System.Linq;

namespace ControlCatalog.Pages
{
    public class WindowPage : UserControl
    {
        public WindowPage()
        {
            this.InitializeComponent();

            DataContext = this;

            OpenWindowCommand = ReactiveCommand.Create(() =>
            {
                new WindowContent().Show();                
            });

            OpenModalWindowCommand = ReactiveCommand.Create(() =>
            {
                new WindowContent().ShowDialog();
            });

            OpenOwnedWindowCommand = ReactiveCommand.Create(() =>
            {
                new WindowContent()
                {
                    Owner = Window.OpenWindows.First()
                }.Show();
            });

            OpenOwnedModalWindowCommand = ReactiveCommand.Create(() =>
            {
                new WindowContent()
                {
                    Owner = Window.OpenWindows.First()
                }.ShowDialog();
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }       
        
        public ReactiveCommand OpenWindowCommand { get; }

        public ReactiveCommand OpenModalWindowCommand { get; }

        public ReactiveCommand OpenOwnedWindowCommand { get; }

        public ReactiveCommand OpenOwnedModalWindowCommand { get; }
    }
}
