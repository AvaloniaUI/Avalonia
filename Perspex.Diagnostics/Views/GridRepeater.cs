// -----------------------------------------------------------------------
// <copyright file="VisualTreeView.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.Views
{
    using Perspex.Controls;
    using Perspex.Controls.Templates;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal static class GridRepeater
    {
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.RegisterAttached<Grid, IEnumerable>("Items", typeof(GridRepeater));

        public static readonly PerspexProperty<Func<object, IEnumerable<Control>>> TemplateProperty =
            PerspexProperty.RegisterAttached<Grid, Func<object, IEnumerable<Control>>>("Template", typeof(GridRepeater));

        static GridRepeater()
        {
            ItemsProperty.Changed.Subscribe(ItemsChanged);
        }

        private static void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            var grid = (Grid)e.Sender;
            var items = (IEnumerable)e.NewValue;
            var template = grid.GetValue(TemplateProperty);

            grid.Children.Clear();

            if (items != null)
            {
                int count = 0;
                int cols = grid.ColumnDefinitions.Count;

                foreach (var item in items)
                {
                    foreach (var control in template(item))
                    {
                        grid.Children.Add(control);
                        Grid.SetColumn(control, count % cols);
                        Grid.SetRow(control, count / cols);
                        ++count;
                    }
                }

                int rows = (int)Math.Ceiling((double)count / cols);
                int difference = rows - grid.RowDefinitions.Count;

                if (difference > 0)
                {
                    for (int i = 0; i < difference; ++i)
                    {
                        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    }
                }
                else if (difference < 0)
                {
                    for (int i = 0; i < difference; ++i)
                    {
                        grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
                    }
                }
            }
        }
    }
}
