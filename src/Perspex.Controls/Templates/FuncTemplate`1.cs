





namespace Perspex.Controls.Templates
{
    using System;

    /// <summary>
    /// Creates a control from a <see cref="Func{TControl}"/>.
    /// </summary>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public class FuncTemplate<TControl> : ITemplate<TControl> where TControl : IControl
    {
        private Func<TControl> func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTemplate{TControl}"/> class.
        /// </summary>
        /// <param name="func">The function used to create the control.</param>
        public FuncTemplate(Func<TControl> func)
        {
            Contract.Requires<ArgumentNullException>(func != null);

            this.func = func;
        }

        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <returns>
        /// The created control.
        /// </returns>
        public TControl Build()
        {
            return this.func();
        }
    }
}