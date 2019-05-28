using Avalonia.Collections;
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

            if(true) // todo: check to see if we have native menu exporter available.
            {

            }
            else
            {

            }
        }
    }
}
