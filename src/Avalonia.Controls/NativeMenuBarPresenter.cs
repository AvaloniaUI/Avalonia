using System;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Controls;

internal class NativeMenuBarPresenter : Menu
{
    protected override Type StyleKeyOverride => typeof(Menu);

    internal static Control? CreateContainerForNativeItem(object? item, int index, object? recycleKey)
    {
        if (item is NativeMenuItemSeparator)
        {
            return new Separator();
        }
        else if (item is NativeMenuItem nativeItem)
        {
            var newItem = new NativeMenuItemPresenter
            {
                ItemsSource = nativeItem.Menu?.Items,
                [!HeaderedSelectingItemsControl.HeaderProperty] =
                    nativeItem.GetObservable(NativeMenuItem.HeaderProperty).ToBinding(),
                [!MenuItem.IconProperty] = nativeItem.GetObservable(NativeMenuItem.IconProperty)
                    .Select(i => i is { } bitmap ? new Image { Source = bitmap } : null).ToBinding(),
                [!MenuItem.IsEnabledProperty] = nativeItem.GetObservable(NativeMenuItem.IsEnabledProperty).ToBinding(),
                [!MenuItem.IsVisibleProperty] = nativeItem.GetObservable(NativeMenuItem.IsVisibleProperty).ToBinding(),
                [!MenuItem.CommandProperty] = nativeItem.GetObservable(NativeMenuItem.CommandProperty).ToBinding(),
                [!MenuItem.CommandParameterProperty] =
                    nativeItem.GetObservable(NativeMenuItem.CommandParameterProperty).ToBinding(),
                [!MenuItem.InputGestureProperty] = nativeItem.GetObservable(NativeMenuItem.GestureProperty).ToBinding(),
                [!MenuItem.ToggleTypeProperty] = nativeItem.GetObservable(NativeMenuItem.ToggleTypeProperty)
                    // TODO12 remove NativeMenuItemToggleType
                    .Select(v => (MenuItemToggleType)v).ToBinding()
            };

            BindingOperations.Apply(newItem, MenuItem.IsCheckedProperty, InstancedBinding.TwoWay(
                nativeItem.GetObservable(NativeMenuItem.IsCheckedProperty).Select(v => (object)v),
                new AnonymousObserver<object?>(v => nativeItem.SetValue(NativeMenuItem.IsCheckedProperty, v))));

            newItem.Click += MenuItemOnClick;

            return newItem;
        }

        return null;

        static void MenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender!).DataContext is NativeMenuItem item
                && item.HasClickHandlers && item is INativeMenuItemExporterEventsImplBridge bridge)
            {
                bridge.RaiseClicked();
            }
        }
    }

    protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return CreateContainerForNativeItem(item, index, recycleKey)
               ?? base.CreateContainerForItemOverride(item, index, recycleKey);
    }

    private class NativeMenuItemPresenter : MenuItem
    {
        protected override Type StyleKeyOverride => typeof(MenuItem);

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return CreateContainerForNativeItem(item, index, recycleKey)
                   ?? base.CreateContainerForItemOverride(item, index, recycleKey);
        }
    }
}
