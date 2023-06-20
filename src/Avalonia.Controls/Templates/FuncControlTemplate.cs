using System;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A template for a <see cref="TemplatedControl"/>.
    /// </summary>
    public class FuncControlTemplate : FuncTemplate<TemplatedControl, Control>, IControlTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncControlTemplate"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public FuncControlTemplate(Func<TemplatedControl, INameScope, Control> build)
            : base(build)
        {
        }

        public new TemplateResult<Control> Build(TemplatedControl param)
        {
            var (control, scope) = BuildWithNameScope(param);
            return new(control, scope);
        }
    }
}
