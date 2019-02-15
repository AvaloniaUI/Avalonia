// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
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

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            Order.Add($"Property {e.Property.Name} Changed");
            base.OnPropertyChanged(e);
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
