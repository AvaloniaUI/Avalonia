using System;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Creates a control from a <see cref="Func{TControl}"/>.
    /// </summary>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public class FuncTemplate<TControl> : ITemplate<TControl> where TControl : Control?
    {
        private readonly Func<TControl> _func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTemplate{TControl}"/> class.
        /// </summary>
        /// <param name="func">The function used to create the control.</param>
        public FuncTemplate(Func<TControl> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <returns>
        /// The created control.
        /// </returns>
        public TControl Build()
        {
            return _func();
        }

        object? ITemplate.Build() => Build();
    }
}
