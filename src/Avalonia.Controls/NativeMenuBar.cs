using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    [TemplatePart("PART_NativeMenuPresenter", typeof(MenuBase))]
    public class NativeMenuBar : TemplatedControl
    {
        [Unstable("To be removed in 12.0, NativeMenuBar now has a default template")] // TODO12
        public static readonly AttachedProperty<bool> EnableMenuItemClickForwardingProperty =
            AvaloniaProperty.RegisterAttached<NativeMenuBar, MenuItem, bool>(
                "EnableMenuItemClickForwarding");

        private MenuBase? _menu;
        private IDisposable? _subscriptions;

        static NativeMenuBar()
        {
            EnableMenuItemClickForwardingProperty.Changed.Subscribe(args =>
            {
                var item = (MenuItem)args.Sender;
                if (args.NewValue.GetValueOrDefault<bool>())
                    item.Click += OnMenuItemClick;
                else
                    item.Click -= OnMenuItemClick;
            });

            // TODO12 Ideally we should make NativeMenuBar inherit MenuBase directly, but it would be a breaking change for 11.x.
            // Changing default template while keeping old StyleKeyOverride => Menu isn't a breaking change.
            TemplateProperty.OverrideDefaultValue<NativeMenuBar>(new FuncControlTemplate((_, ns) => new NativeMenuBarPresenter
            {
                Name = "PART_NativeMenuPresenter",
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty)
            }.RegisterInNameScope(ns)));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _menu = e.NameScope.Find<MenuBase>("PART_NativeMenuPresenter")
                    ?? this.FindDescendantOfType<MenuBase>()
                    ?? throw new InvalidOperationException("NativeMenuBar requires a MenuBase#PART_NativeMenuPresenter template part.");
            
            if (VisualRoot is TopLevel topLevel)
            {
                SubscribeToToplevel(topLevel, _menu);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_menu is null)
                return;

            if (e.Root is TopLevel topLevel)
            {
                SubscribeToToplevel(topLevel, _menu);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        [Unstable("To be removed in 12.0, NativeMenuBar now has a default template.")] // TODO12
        public static void SetEnableMenuItemClickForwarding(MenuItem menuItem, bool enable)
        {
            menuItem.SetValue(EnableMenuItemClickForwardingProperty, enable);
        }

        private static void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            (((MenuItem)sender!).DataContext as INativeMenuItemExporterEventsImplBridge)?.RaiseClicked();
        }

        private void SubscribeToToplevel(TopLevel topLevel, MenuBase menu)
        {
            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable(
                menu.Bind(IsVisibleProperty, topLevel.GetBindingObservable(NativeMenu.IsNativeMenuExportedProperty)
                    .Select(v => !v.GetValueOrDefault<bool>())),
                menu.Bind(ItemsControl.ItemsSourceProperty, topLevel.GetBindingObservable(NativeMenu.MenuProperty)
                    .Select(v => v.GetValueOrDefault<NativeMenu>()?.Items)));
        }
    }
}
