// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    /// <summary>
    /// Recieves input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    public interface IInputManager
    {
        /// <summary>
        /// Gets an observable that notifies on each input event recieved before
        /// <see cref="Process"/>.
        /// </summary>
        IObservable<RawInputEventArgs> PreProcess { get; }

        /// <summary>
        /// Gets an observable that notifies on each input event recieved.
        /// </summary>
        IObservable<RawInputEventArgs> Process { get; }

        /// <summary>
        /// Gets an observable that notifies on each input event recieved after
        /// <see cref="Process"/>.
        /// </summary>
        IObservable<RawInputEventArgs> PostProcess { get; }

        /// <summary>
        /// Processes a raw input event.
        /// </summary>
        /// <param name="e">The raw input event.</param>
        void ProcessInput(RawInputEventArgs e);
    }
}
