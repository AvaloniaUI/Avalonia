





namespace Perspex.Controls.Templates
{
    using System;

    /// <summary>
    /// Creates a control from a <see cref="Func{TParam, TControl}"/>.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter.</typeparam>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public class FuncTemplate<TParam, TControl> : ITemplate<TParam, TControl>
        where TControl : IControl
    {
        private Func<TParam, TControl> func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTemplate{TControl, TParam}"/> class.
        /// </summary>
        /// <param name="func">The function used to create the control.</param>
        public FuncTemplate(Func<TParam, TControl> func)
        {
            Contract.Requires<ArgumentNullException>(func != null);

            this.func = func;
        }

        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <returns>
        /// The created control.
        /// </returns>
        public TControl Build(TParam param)
        {
            return this.func(param);
        }
    }
}