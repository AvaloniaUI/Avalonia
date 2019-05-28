using Avalonia.Collections;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public class NativeMenu : Control
    {
        [Content]
        public AvaloniaList<NativeMenuItem> Items { get; set; } = new AvaloniaList<NativeMenuItem>();

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var exporter = AvaloniaLocator.Current.GetService<INativeMenuExporter>();

            if (exporter != null)
            {
                exporter.SetMenu(Items);
            }
            else
            {
                // TODO decorate a Menu and add a template for NativeMenu items.
            }
        }
    }
}
