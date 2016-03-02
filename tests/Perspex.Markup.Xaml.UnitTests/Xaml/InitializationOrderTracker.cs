// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Perspex.Controls;
using Perspex.LogicalTree;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class InitializationOrderTracker : Control
    {
        public IList<string> Order { get; } = new List<string>();

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            Order.Add("AttachedToLogicalTree");
            base.OnAttachedToLogicalTree(e);
        }

        protected override void OnPropertyChanged(PerspexPropertyChangedEventArgs e)
        {
            Order.Add($"Property {e.Property.Name} Changed");
            base.OnPropertyChanged(e);
        }
    }
}
