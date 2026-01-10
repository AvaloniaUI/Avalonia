using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// A collection of <see cref="GradientStop"/>s.
    /// </summary>
    public class GradientStops : AvaloniaList<GradientStop>
    {
        public GradientStops()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public IReadOnlyList<ImmutableGradientStop> ToImmutable()
        {
            var count = Count;
            var stops = new ImmutableGradientStop[count];

            for (var i = 0; i < count; i++)
            {
                var currentStop = this[i];

                stops[i] = new ImmutableGradientStop(currentStop.Offset, currentStop.Color);
            }

            return stops;
        }
    }
}
