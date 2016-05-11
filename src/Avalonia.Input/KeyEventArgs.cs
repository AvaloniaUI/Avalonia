// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class KeyEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public Key Key { get; set; }

        public InputModifiers Modifiers { get; set; }
    }
}
