// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a text decoration, which is a visual ornamentation that is added to text (such as an underline).
    /// </summary>
    public class TextDecoration : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Location"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationLocation> LocationProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationLocation>(nameof(Location));

        /// <summary>
        /// Defines the <see cref="Pen"/> property.
        /// </summary>
        public static readonly StyledProperty<IPen> PenProperty =
            AvaloniaProperty.Register<TextDecoration, IPen>(nameof(Pen));

        /// <summary>
        /// Defines the <see cref="PenThicknessUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> PenThicknessUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(PenThicknessUnit));

        /// <summary>
        /// Defines the <see cref="PenOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> PenOffsetProperty =
            AvaloniaProperty.Register<TextDecoration, double>(nameof(PenOffset));

        /// <summary>
        /// Defines the <see cref="PenOffsetUnit"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationUnit> PenOffsetUnitProperty =
            AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>(nameof(PenOffsetUnit));

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public TextDecorationLocation Location
        {
            get => GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets or sets the pen.
        /// </summary>
        /// <value>
        ///     The pen.
        /// </value>
        public IPen Pen
        {
            get => GetValue(PenProperty);
            set => SetValue(PenProperty, value);
        }

        /// <summary>
        /// Gets the units in which the Thickness of the text decoration's <see cref="Pen"/> is expressed.
        /// </summary>
        public TextDecorationUnit PenThicknessUnit
        {
            get => GetValue(PenThicknessUnitProperty);
            set => SetValue(PenThicknessUnitProperty, value);
        }

        /// <summary>
        /// Gets or sets the pen offset.
        /// </summary>
        /// <value>
        /// The pen offset.
        /// </value>
        public double PenOffset
        {
            get => GetValue(PenOffsetProperty);
            set => SetValue(PenOffsetProperty, value);
        }

        /// <summary>
        /// Gets the units in which the <see cref="PenOffset"/> value is expressed.
        /// </summary>
        public TextDecorationUnit PenOffsetUnit
        {
            get => GetValue(PenOffsetUnitProperty);
            set => SetValue(PenOffsetUnitProperty, value);
        }

        /// <summary>
        /// Creates an immutable clone of the <see cref="TextDecoration"/>.
        /// </summary>
        /// <returns>The immutable clone.</returns>
        public ImmutableTextDecoration ToImmutable()
        {
            return new ImmutableTextDecoration(Location, Pen?.ToImmutable(), PenThicknessUnit, PenOffset, PenOffsetUnit);
        }
    }
}
