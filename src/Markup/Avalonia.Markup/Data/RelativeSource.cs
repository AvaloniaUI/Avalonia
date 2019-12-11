// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Defines the mode of a <see cref="RelativeSource"/> object.
    /// </summary>
    public enum RelativeSourceMode
    {
        /// <summary>
        /// The binding will be to the control's data context.
        /// </summary>
        DataContext,

        /// <summary>
        /// The binding will be to the control's templated parent.
        /// </summary>
        TemplatedParent,

        /// <summary>
        /// The binding will be to the control itself.
        /// </summary>
        Self,

        /// <summary>
        /// The binding will be to an ancestor of the control in the visual tree.
        /// </summary>
        FindAncestor,
    }


    /// <summary>
    /// The type of tree via which to track a control.
    /// </summary>
    public enum TreeType
    {
        /// <summary>
        /// The visual tree.
        /// </summary>
        Visual,
        /// <summary>
        /// The logical tree.
        /// </summary>
        Logical,
    }

    /// <summary>
    /// Describes the the location of a binding source, relative to the binding target.
    /// </summary>
    public class RelativeSource
    {
        private int _ancestorLevel = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeSource"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes <see cref="Mode"/> to <see cref="RelativeSourceMode.FindAncestor"/>.
        /// </remarks>
        public RelativeSource()
        {
            Mode = RelativeSourceMode.FindAncestor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeSource"/> class.
        /// </summary>
        /// <param name="mode">The relative source mode.</param>
        public RelativeSource(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// Gets the level of ancestor to look for when in <see cref="RelativeSourceMode.FindAncestor"/>  mode.
        /// </summary>
        /// <remarks>
        /// Use the default value of 1 to look for the first ancestor of the specified type.
        /// </remarks>
        public int AncestorLevel
        {
            get { return _ancestorLevel; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "AncestorLevel may not be set to less than 1.");
                }

                _ancestorLevel = value;
            }
        }

        /// <summary>
        /// Gets the type of ancestor to look for when in <see cref="RelativeSourceMode.FindAncestor"/>  mode.
        /// </summary>
        public Type AncestorType { get; set; }

        /// <summary>
        /// Gets or sets a value that describes the type of relative source lookup.
        /// </summary>
        public RelativeSourceMode Mode { get; set; }

        public TreeType Tree { get; set; } = TreeType.Visual;
    }
}
