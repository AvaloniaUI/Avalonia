// -----------------------------------------------------------------------
// <copyright file="IKeyboardNavigation.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public interface IKeyboardNavigation
    {
        IInputElement GetNextInTabOrder(IInputElement element);

        IInputElement GetPreviousInTabOrder(IInputElement element);

        void TabNext(IInputElement element);

        void TabPrevious(IInputElement element);

        void TabTo(IInputElement element);
    }
}