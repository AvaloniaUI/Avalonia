// -----------------------------------------------------------------------
// <copyright file="ControlTemplate.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates
{
    using System;
    using Perspex.Controls.Primitives;
    using Perspex.Styling;

    /// <summary>
    /// A template for a <see cref="TemplatedControl"/>.
    /// </summary>
    public class ControlTemplate : FuncTemplate<ITemplatedControl, Control>, IControlTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlTemplate"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public ControlTemplate(Func<ITemplatedControl, Control> build)
            : base(build)
        {
        }
    }
}