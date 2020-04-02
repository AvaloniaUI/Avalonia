using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class InitializationOrderTracker : Control, ISupportInitialize
    {
        public IList<string> Order { get; } = new List<string>();

        public int InitState { get; private set; }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            Order.Add("AttachedToLogicalTree");
            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnPropertyChanged<T>(AvaloniaProperty<T> property, Optional<T> oldValue, BindingValue<T> newValue, BindingPriority priority)
        {
            Order.Add($"Property {property.Name} Changed");
            base.OnPropertyChanged(property, oldValue, newValue, priority);
        }

        void ISupportInitialize.BeginInit()
        {
            ++InitState;
            base.BeginInit();
            Order.Add($"BeginInit {InitState}");
        }

        void ISupportInitialize.EndInit()
        {
            --InitState;
            base.EndInit();
            Order.Add($"EndInit {InitState}");
        }
    }
}
