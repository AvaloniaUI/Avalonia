// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    [Obsolete("Use TextInputHandlerSelection")]
    public class TextInputEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public string Text { get; set; }
    }
}
