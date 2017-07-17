// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Input
{
    /// <summary>
    /// Designates a control as handling its own keyboard navigation.
    /// </summary>
    public interface ICustomKeyboardNavigation
    {
        (bool handled, IInputElement next) GetNext(IInputElement element, NavigationDirection direction);
    }
}
