// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for <see cref="ColumnDefinition"/> and <see cref="RowDefinition"/>.
    /// </summary>
    public abstract class DefinitionBase : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="SharedSizeGroup"/> property.
        /// </summary>
        public static readonly StyledProperty<string> SharedSizeGroupProperty =
            AvaloniaProperty.Register<DefinitionBase, string>(nameof(SharedSizeGroup), inherits: true);

        /// <summary>
        /// Gets or sets the name of the shared size group of the column or row.
        /// </summary>
        public string SharedSizeGroup
        {
            get { return GetValue(SharedSizeGroupProperty); }
            set { SetValue(SharedSizeGroupProperty, value); }
        }

        /// <summary>
        /// Internal helper to access up-to-date UserSize property value.
        /// </summary>
        internal abstract GridLength UserSize { get; }

        /// <summary>
        /// Internal helper to access up-to-date UserMinSize property value.
        /// </summary>
        internal abstract double UserMinSize { get; }

        /// <summary>
        /// Internal helper to access up-to-date UserMaxSize property value.
        /// </summary>
        internal abstract double UserMaxSize { get; }

        /// <summary>
        /// Layout-time user size type.
        /// </summary>
        internal Grid.LayoutTimeSizeType SizeType { get; set; }
        
        /// <summary>
        /// Returns or sets measure size for the definition.
        /// </summary>
        internal double MeasureSize { get; set; }

        /// <summary>
        /// Returns definition's layout time type sensitive preferred size.
        /// </summary>
        /// <remarks>
        /// Returned value is guaranteed to be true preferred size.
        /// </remarks>
        internal double PreferredSize
        {
            get
            {
                double preferredSize = MinSize;
                if (SizeType != Grid.LayoutTimeSizeType.Auto
                    && preferredSize < MeasureSize)
                {
                    preferredSize = MeasureSize;
                }
                return (preferredSize);
            }
        }

        /// <summary>
        /// Returns or sets size cache for the definition.
        /// </summary>
        internal double SizeCache { get; set; }

        /// <summary>
        /// Used during measure and arrange to accumulate size for "Auto" and "Star" DefinitionBase's
        /// </summary>
        internal double MinSize { get; set; }

        /// <summary>
        /// Updates min size.
        /// </summary>
        /// <param name="minSize">New size.</param>
        internal void UpdateMinSize(double minSize)
        {
            MinSize = Math.Max(MinSize, minSize);
        }

        /// <summary>
        /// Offset.
        /// </summary>
        internal double FinalOffset { get; set; }
    }
}