





namespace Perspex.Controls.Templates
{
    using System;
    using Perspex.Controls.Primitives;
    using Styling;

    /// <summary>
    /// A template for a <see cref="TemplatedControl"/>.
    /// </summary>
    /// <typeparam name="T">The type of the lookless control.</typeparam>
    public class ControlTemplate<T> : ControlTemplate where T : ITemplatedControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">The build function.</param>
        public ControlTemplate(Func<T, IControl> build)
            : base(x => build((T)x))
        {
        }
    }
}