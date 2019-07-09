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
    public class FuncControlTemplate : FuncTemplate<ITemplatedControl, IControl>, IControlTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncControlTemplate"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public FuncControlTemplate(Func<ITemplatedControl, INameScope, IControl> build)
            : base(build)
        {
        }

        public new ControlTemplateResult Build(ITemplatedControl param)
        {
            var (control, scope) = BuildWithNameScope(param);
            return new ControlTemplateResult(control, scope);
        }
    }
}
