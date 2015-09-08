// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Input
{
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

        void SetFocusedElement(IInputElement element, NavigationMethod method);
    }
}
