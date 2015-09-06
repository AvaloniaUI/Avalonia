// -----------------------------------------------------------------------
// <copyright file="Brush.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    public abstract class Brush : PerspexObject
    {
        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> OpacityProperty =
            PerspexProperty.Register<Brush, double>(nameof(Opacity), 1.0);

        /// <summary>
        /// Gets or sets the opacity of the brush.
        /// </summary>
        public double Opacity
        {
            get { return this.GetValue(OpacityProperty); }
            set { this.SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Parses a brush string.
        /// </summary>
        /// <param name="s">The brush string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Brush Parse(string s)
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
                    return (Brush)member.GetValue(null);
                }
                else
                {
                    throw new FormatException($"Invalid brush string: '{s}'.");
                }
            }
        }
    }
}
