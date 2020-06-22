using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Size"/> type.
    /// </summary>  
    public class SizeTransition : Transition<Size>
    {
        /// <inheritdocs/>
        public override IObservable<Size> DoTransition(IObservable<double> progress, Size oldValue, Size newValue)
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
