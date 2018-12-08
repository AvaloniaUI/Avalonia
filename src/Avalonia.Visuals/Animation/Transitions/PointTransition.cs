// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            var deltaX = newValue.X - oldValue.Y;
            var deltaY = newValue.X - oldValue.Y;

            return progress
                .Select(p =>
                {
                    var f = Easing.Ease(p);
                    var nX = f * deltaX + oldValue.X;
                    var nY = f * deltaY + oldValue.Y;
                    return new Point(nX, nY);
                });
        }
    }
}
