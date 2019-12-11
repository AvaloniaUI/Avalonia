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

        [Obsolete("Use KeyModifiers")]
        public InputModifiers Modifiers => (InputModifiers)KeyModifiers;
        public KeyModifiers KeyModifiers { get; set; }
    }
}
