// -----------------------------------------------------------------------
// <copyright file="IMenu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    internal interface IMenu
    {
        void ChildPointerEnter(MenuItem item);

        void ChildSubMenuOpened(MenuItem item);
    }
}
