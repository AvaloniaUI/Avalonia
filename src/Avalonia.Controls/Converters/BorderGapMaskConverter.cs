// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Converters
{
    // Ported from https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/BorderGapMaskConverter.cs

    /// <summary>
    /// BorderGapMaskConverter class
    /// </summary>
    public class BorderGapMaskConverter : IMultiValueConverter
    {
        /// <summary>
        /// Convert a value.
        /// </summary>
        /// <param name="values">values as produced by source binding</param>
        /// <param name="targetType">target type</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>
        /// Converted value.
        /// Visual Brush that is used as the opacity mask for the Border
        /// in the style for GroupBox.
        /// </returns>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            //
            // Parameter Validation
            //
            if (parameter == null ||
                values == null ||
                values.Count != 3 ||
                values[0] is not double ||
                values[1] is not double ||
                values[2] is not double)
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (parameter is not double && parameter is not string)
            {
                return AvaloniaProperty.UnsetValue;
            }

            //
            // Conversion
            //
            double? headerWidth = (double?)values[0];
            double? borderWidth = (double?)values[1];
            double? borderHeight = (double?)values[2];

            // Doesn't make sense to have a Grid
            // with 0 as width or height
            if (borderWidth == 0
                || borderHeight == 0)
            {
                return null;
            }

            // Width of the line to the left of the header
            // to be used to set the width of the first column of the Grid
            double lineWidth;
            if (parameter is string)
            {
                lineWidth = Double.Parse(((string)parameter), NumberFormatInfo.InvariantInfo);
            }
            else
            {
                lineWidth = (double)parameter;
            }

            Grid grid = new Grid
            {
                Width = borderWidth ?? 0,
                Height = borderHeight ?? 0
            };

            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            colDef1.Width = new GridLength(lineWidth);
            colDef2.Width = new GridLength(headerWidth ?? 0);
            colDef3.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(colDef1);
            grid.ColumnDefinitions.Add(colDef2);
            grid.ColumnDefinitions.Add(colDef3);
            RowDefinition rowDef1 = new RowDefinition();
            RowDefinition rowDef2 = new RowDefinition();
            rowDef1.Height = new GridLength((borderHeight ?? 0) / 2);
            rowDef2.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(rowDef1);
            grid.RowDefinitions.Add(rowDef2);

            Rectangle rectColumn1 = new Rectangle();
            Rectangle rectColumn2 = new Rectangle();
            Rectangle rectColumn3 = new Rectangle();
            rectColumn1.Fill = Brushes.Black;
            rectColumn2.Fill = Brushes.Black;
            rectColumn3.Fill = Brushes.Black;

            Grid.SetRowSpan(rectColumn1, 2);
            Grid.SetRow(rectColumn1, 0);
            Grid.SetColumn(rectColumn1, 0);

            Grid.SetRow(rectColumn2, 1);
            Grid.SetColumn(rectColumn2, 1);

            Grid.SetRowSpan(rectColumn3, 2);
            Grid.SetRow(rectColumn3, 0);
            Grid.SetColumn(rectColumn3, 2);

            grid.Children.Add(rectColumn1);
            grid.Children.Add(rectColumn2);
            grid.Children.Add(rectColumn3);

            return (new VisualBrush(grid));
        }
    }
}
