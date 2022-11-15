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

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            Order.Add($"Property {change.Property.Name} Changed");
            base.OnPropertyChanged(change);
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
