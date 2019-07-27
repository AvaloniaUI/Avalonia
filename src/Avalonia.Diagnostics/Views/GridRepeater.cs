// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Views
{
    internal static class GridRepeater
    {
        public static readonly AttachedProperty<IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterAttached<SimpleGrid, IEnumerable>("Items", typeof(GridRepeater));

        public static readonly AttachedProperty<Func<object, IEnumerable<Control>>> TemplateProperty =
            AvaloniaProperty.RegisterAttached<SimpleGrid, Func<object, IEnumerable<Control>>>("Template",
                typeof(GridRepeater));

        static GridRepeater()
        {
            ItemsProperty.Changed.Subscribe(ItemsChanged);
        }

        private static void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var grid = (SimpleGrid)e.Sender;
            var items = (IEnumerable)e.NewValue;
            var template = grid.GetValue(TemplateProperty);

            grid.Children.Clear();

            if (items != null)
            {
                int count = 0;
                int cols = 3;

                foreach (var item in items)
                {
                    foreach (var control in template(item))
                    {
                        grid.Children.Add(control);
                        SimpleGrid.SetColumn(control, count % cols);
                        SimpleGrid.SetRow(control, count / cols);
                        ++count;
                    }
                }
            }
        }
    }
}
