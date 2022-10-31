using System;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A template for a <see cref="TemplatedControl"/>.
    /// </summary>
    /// <typeparam name="T">The type of the lookless control.</typeparam>
    public class FuncControlTemplate<T> : FuncControlTemplate where T : TemplatedControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncControlTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public FuncControlTemplate(Func<T, INameScope, Control> build)
            : base((x, s) => build((T)x, s))
        {
        }
    }
}
