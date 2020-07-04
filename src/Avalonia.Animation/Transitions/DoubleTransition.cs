using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        /// <inheritdocs/>
        public override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue)
        {
            return progress
                .Select(p =>
                {
                    var f = Easing.Ease(p);
                    return ((newValue - oldValue) * f) + oldValue;
                });
        }
    }
}
