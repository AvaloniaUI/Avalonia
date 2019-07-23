using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Dialogs.Internal
{
    class ManagedFileDialog : Window
    {
        private ManagedFileChooserViewModel _model;
        public ManagedFileDialog()
        {
            AvaloniaXamlLoader.Load(this);
            #if DEBUG
                this.AttachDevTools();
            #endif
        }
    }
}
