using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
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
        private static FuncDataTemplate DefaultItemTemplate = new FuncDataTemplate<object>((x, _) =>
        {
            var control = new ViewModelViewHost();
            var context = control.GetObservable(Control.DataContextProperty);
            control.Bind(ViewModelViewHost.ViewModelProperty, context);
            control.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            control.VerticalContentAlignment = VerticalAlignment.Stretch;
            return control;
        },
        true);

        /// <inheritdoc/>
        public bool ExecuteHook(
            object? source, object target,
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

            if (itemsControl.DataTemplates != null &&
                itemsControl.DataTemplates.Count > 0)
                return true;

            itemsControl.ItemTemplate = DefaultItemTemplate;
            return true;
        }
    }
}
