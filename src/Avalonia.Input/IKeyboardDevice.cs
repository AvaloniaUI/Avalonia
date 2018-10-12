// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;

namespace Avalonia.Input
{
    [Flags]
    public enum InputModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        Command = 16, // Special case CMD on OSX, CTRL on other platforms.
        LeftMouseButton = 32,
        RightMouseButton = 64,
        MiddleMouseButton = 128
    }

    [Flags]
    public enum KeyStates
    {
        None = 0,
        Down = 1,
        Toggled = 2,
    }

    public interface IKeyboardDevice : IInputDevice, INotifyPropertyChanged
    {
        IInputElement FocusedElement { get; }

        void SetFocusedElement(
            IInputElement element, 
            NavigationMethod method,
            InputModifiers modifiers);
    }
}
