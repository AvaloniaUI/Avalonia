using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.iOS
{
    public class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public AvaloniaView View;

        public Control MainView
        {
            get => View.Content;
            set => View.Content = value;
        }
    }
}
