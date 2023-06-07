using System;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Creates a control from a <see cref="Func{TParam, TControl}"/>.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter.</typeparam>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public class FuncTemplate<TParam, TControl> : ITemplate<TParam, TControl>
        where TControl : Control?
    {
        private readonly Func<TParam, INameScope, TControl> _func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTemplate{TControl, TParam}"/> class.
        /// </summary>
        /// <param name="func">The function used to create the control.</param>
        public FuncTemplate(Func<TParam, INameScope, TControl> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
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
            return BuildWithNameScope(param).control;
        }

        protected (TControl control, INameScope nameScope) BuildWithNameScope(TParam param)
        {
            var scope = new NameScope();
            var rv = _func(param, scope);
            return (rv, scope);
        }
    }
}
