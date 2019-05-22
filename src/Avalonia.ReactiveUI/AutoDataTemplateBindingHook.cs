using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using ReactiveUI;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// AutoDataTemplateBindingHook is a binding hook that checks ItemsControls
    /// that don't have DataTemplates, and assigns a default DataTemplate that
    /// loads the View associated with each ViewModel.
    /// </summary>
    public class AutoDataTemplateBindingHook : IPropertyBindingHook
    {
        private static Lazy<DataTemplate> DefaultItemTemplate { get; } = new Lazy<DataTemplate>(() =>
        {
            var template = @"
<DataTemplate xmlns='https://github.com/avaloniaui'
              xmlns:reactiveUi='http://reactiveui.net'>
    <reactiveUi:ViewModelViewHost
        ViewModel='{Binding Mode=OneWay}'
        VerticalContentAlignment='Stretch'
        HorizontalContentAlignment='Stretch' />
</DataTemplate>";
            
            var loader = new AvaloniaXamlLoader();
            return (DataTemplate)loader.Load(template);
        });

        /// <inheritdoc/>
        public bool ExecuteHook(
            object source, object target,
            Func<IObservedChange<object, object>[]> getCurrentViewModelProperties,
            Func<IObservedChange<object, object>[]> getCurrentViewProperties,
            BindingDirection direction)
        {
            var viewProperties = getCurrentViewProperties();
            var lastViewProperty = viewProperties.LastOrDefault();
            var itemsControl = lastViewProperty?.Sender as ItemsControl;
            if (itemsControl == null)
                return true;

            var propertyName = viewProperties.Last().GetPropertyName();
            if (propertyName != "Items" &&
                propertyName != "ItemsSource")
                return true;
            
            if (itemsControl.ItemTemplate != null)
                return true;

            itemsControl.ItemTemplate = DefaultItemTemplate.Value;
            return true;
        }
    }
}