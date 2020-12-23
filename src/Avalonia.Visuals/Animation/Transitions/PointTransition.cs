using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Point"/> type.
    /// </summary>  
    public class PointTransition : Transition<Point>
    {
        /// <inheritdocs/>
        public override IObservable<Point> DoTransition(IObservable<double> progress, Point oldValue, Point newValue)
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
