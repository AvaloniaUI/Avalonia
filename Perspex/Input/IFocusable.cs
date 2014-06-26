// -----------------------------------------------------------------------
// <copyright file="IFocusable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public interface IFocusable
    {
        bool Focusable { get; }

        bool IsFocused { get; }

        void Focus();
    }
}
