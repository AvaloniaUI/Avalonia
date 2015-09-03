// -----------------------------------------------------------------------
// <copyright file="Brush.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    public abstract class Brush : PerspexObject
    {
        public static readonly PerspexProperty<double> OpacityProperty =
    PerspexProperty.Register<Brush, double>(nameof(Opacity), 1.0);

        public double Opacity
        {
            get { return this.GetValue(OpacityProperty); }
            set { this.SetValue(OpacityProperty, value); }
        }
    }
}
