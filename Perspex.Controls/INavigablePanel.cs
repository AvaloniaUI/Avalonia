// -----------------------------------------------------------------------
// <copyright file="INavigablePanel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Input;

    public interface INavigablePanel
    {
        Control GetControl(FocusNavigationDirection direction, Control from);
    }
}
