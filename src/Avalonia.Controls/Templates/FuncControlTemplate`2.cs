// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A template for a <see cref="TemplatedControl"/>.
    /// </summary>
    /// <typeparam name="T">The type of the lookless control.</typeparam>
    public class FuncControlTemplate<T> : FuncControlTemplate where T : ITemplatedControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncControlTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public FuncControlTemplate(Func<T, INameScope, IControl> build)
            : base((x, s) => build((T)x, s))
        {
        }
    }
}
