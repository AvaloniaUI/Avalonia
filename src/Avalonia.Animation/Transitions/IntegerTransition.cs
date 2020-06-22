using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="int"/> types.
    /// </summary>  
    public class IntegerTransition : Transition<int>
    {
        /// <inheritdocs/>
        public override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue)
        {
            var delta = newValue - oldValue;
            return progress
                .Select(p => (int)(Easing.Ease(p) * delta + oldValue));
        }
    }
}
