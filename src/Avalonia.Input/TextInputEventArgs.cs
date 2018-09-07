// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class TextInputEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public string Text { get; set; }
    }
}
