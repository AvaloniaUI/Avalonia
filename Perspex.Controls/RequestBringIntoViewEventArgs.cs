// -----------------------------------------------------------------------
// <copyright file="TextPresenter.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Interactivity;

    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public IVisual TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
