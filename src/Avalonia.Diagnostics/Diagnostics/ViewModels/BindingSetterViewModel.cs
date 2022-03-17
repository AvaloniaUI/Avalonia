using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class BindingSetterViewModel : SetterViewModel
    {
        public BindingSetterViewModel(AvaloniaProperty property, object? value, string bindingPath, bool isCompiled) : base(property, value)
        {
            Path = bindingPath;
            Tint = isCompiled ? Brushes.DarkGreen : Brushes.CornflowerBlue;
        }

        public IBrush Tint { get; }

        public string Path { get; }

        public override void CopyValue()
        {
            CopyToClipboard(Path);
        }
    }
}
