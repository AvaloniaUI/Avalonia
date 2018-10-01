// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Windows.Markup;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class RelativeSourceExtension : MarkupExtension
    {
        public RelativeSourceExtension()
        {
            Mode = RelativeSourceMode.FindAncestor;
        }

        public RelativeSourceExtension(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new RelativeSource
            {
                AncestorLevel = AncestorLevel,
                AncestorType = AncestorType,
                Mode = Mode,
                Tree = Tree,
            };
        }

        /// <summary>
        /// Gets the level of ancestor to look for when in <see cref="RelativeSourceMode.FindAncestor"/>  mode.
        /// </summary>
        /// <remarks>
        /// Use the default value of 1 to look for the first ancestor of the specified type.
        /// </remarks>
        public int AncestorLevel { get; set; } = 1;

        /// <summary>
        /// Gets the type of ancestor to look for when in <see cref="RelativeSourceMode.FindAncestor"/>  mode.
        /// </summary>
        public Type AncestorType { get; set; }

        /// <summary>
        /// Gets or sets a value that describes the type of relative source lookup.
        /// </summary>
        [ConstructorArgument("mode")]
        public RelativeSourceMode Mode { get; set; }

        public TreeType Tree { get; set; } = TreeType.Visual;
    }
}
