using Avalonia.Input.Platform;
using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ResourceSetterViewModel : SetterViewModel
    {
        public object Key { get; }

        public IBrush Tint { get; }
        
        public string ValueTypeTooltip { get; }

        public ResourceSetterViewModel(AvaloniaProperty property, object resourceKey, object? resourceValue, bool isDynamic, IClipboard? clipboard) : base(property, resourceValue, clipboard)
        {
            Key = resourceKey;
            Tint = isDynamic ? Brushes.Orange : Brushes.Brown;
            ValueTypeTooltip = isDynamic ? "Dynamic Resource" : "Static Resource";
        }

        public void CopyResourceKey()
        {
            var textToCopy = Key?.ToString();

            if (textToCopy is null)
            {
                return;
            }

            CopyToClipboard(textToCopy);
        }
    }
}
