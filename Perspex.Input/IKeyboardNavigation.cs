// -----------------------------------------------------------------------
// <copyright file="IKeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public interface IKeyboardNavigation
    {
        bool MoveNext(IInputElement element);

        bool MovePrevious(IInputElement element);
    }
}