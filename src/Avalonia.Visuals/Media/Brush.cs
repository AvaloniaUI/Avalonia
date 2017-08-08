// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    public abstract class Brush : AvaloniaObject, IBrush
    {
        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<Brush, double>(nameof(Opacity), 1.0);

        /// <summary>
        /// Gets or sets the opacity of the brush.
        /// </summary>
        public double Opacity
        {
            get { return GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Parses a brush string.
        /// </summary>
        /// <param name="s">The brush string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static IBrush Parse(string s)
        {
            if (s[0] == '#')
            {
                return new SolidColorBrush(Color.Parse(s));
            }
            else
            {
                var upper = s.ToUpperInvariant();
                var member = typeof(Brushes).GetTypeInfo().DeclaredProperties
                    .FirstOrDefault(x => x.Name.ToUpperInvariant() == upper);

                if (member != null)
                {
                    var brush = (ISolidColorBrush)member.GetValue(null);
                    return new SolidColorBrush(brush.Color, brush.Opacity);
                }
                else
                {
                    throw new FormatException($"Invalid brush string: '{s}'.");
                }
            }
        }
    }
}
