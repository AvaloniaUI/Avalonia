// -----------------------------------------------------------------------
// <copyright file="IKeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
    }

    [Flags]
    public enum KeyStates
    {
        None = 0,
        Down = 1,
        Toggled = 2,
    }

    public interface IKeyboardDevice : IInputDevice
    {
        IInputElement FocusedElement { get; }

        ModifierKeys Modifiers { get; }

        void SetFocusedElement(IInputElement element, NavigationMethod method);
    }
}
