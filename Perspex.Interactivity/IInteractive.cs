// -----------------------------------------------------------------------
// <copyright file="IInteractive.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity
{
    public interface IInteractive : IVisual
    {
        void RaiseEvent(RoutedEventArgs e);
    }
}
