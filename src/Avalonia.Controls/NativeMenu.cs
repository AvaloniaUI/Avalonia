using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Platform;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public class NativeMenu : Control
    {
        private ICollection<NativeMenuItem> _items = new AvaloniaList<NativeMenuItem>();

        public static readonly StyledProperty<bool> MenuExportedProperty =
            AvaloniaProperty.Register<NativeMenu ,bool>(nameof(MenuExported), true);

        public bool MenuExported
        {
            get => GetValue(MenuExportedProperty);
            set => SetValue(MenuExportedProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<NativeMenu, ICollection<NativeMenuItem>> ItemsProperty =
            AvaloniaProperty.RegisterDirect<NativeMenu, ICollection<NativeMenuItem>>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        public NativeMenu()
        {
            this.GetObservable(ItemsProperty)
                .Where(x=> x != null)
                .Subscribe(x =>
                {
                    UpdateMenu(x);
                });
        }

        private void UpdateMenu(ICollection<NativeMenuItem> items)
        {
            var exporter = AvaloniaLocator.Current.GetService<INativeMenuExporter>();

            if (exporter != null)
            {
                MenuExported = true;
                exporter.SetMenu(items);
            }
            else
            {
                MenuExported = false;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public ICollection<NativeMenuItem> Items
        {
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (Items != null)
            {
                UpdateMenu(Items);
            }
        }
    }
}
