// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    public interface IInputDevice
    {
        /// <summary>
        /// Processes raw event. Is called after preprocessing by InputManager
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="focusedElement"></param>
        void ProcessRawEvent(RawInputEventArgs ev, IInputElement focusedElement);
    }
}
