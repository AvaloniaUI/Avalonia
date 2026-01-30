using System;
using Avalonia.Collections;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// A collection of <see cref="ITransition"/> definitions.
    /// </summary>
    public sealed class Transitions : AvaloniaList<ITransition>, IAvaloniaListItemValidator<ITransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transitions"/> class.
        /// </summary>
        public Transitions()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validator = this;
        }

        void IAvaloniaListItemValidator<ITransition>.Validate(ITransition item)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (item is IPropertyTransition { Property: { IsDirect: true } property })
            {
                var display = item is TransitionBase transition ? transition.DebugDisplay : item.ToString();
                throw new InvalidOperationException($"Cannot animate direct property {property} on {display}.");
            }
        }
    }
}
