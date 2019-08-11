using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public class NativeMenu : Control
    {
        private IEnumerable<NativeMenuItem> _items = new AvaloniaList<NativeMenuItem>();

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<NativeMenu, IEnumerable<NativeMenuItem>> ItemsProperty =
            AvaloniaProperty.RegisterDirect<NativeMenu, IEnumerable<NativeMenuItem>>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable<NativeMenuItem> Items
        {
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var exporter = AvaloniaLocator.Current.GetService<INativeMenuExporter>();

            if (exporter != null)
            {
                exporter.SetMenu(Items);
            }
        }
    }
}
