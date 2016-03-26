// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using Perspex.Input.Raw;

namespace Perspex.Input
{
    /// <summary>
    /// Recieves input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    public class InputManager : IInputManager
    {
        private readonly Subject<RawInputEventArgs> _preProcess = new Subject<RawInputEventArgs>();
        private readonly Subject<RawInputEventArgs> _process = new Subject<RawInputEventArgs>();
        private readonly Subject<RawInputEventArgs> _postProcess = new Subject<RawInputEventArgs>();

        /// <summary>
        /// Gets the global instance of the input manager.
        /// </summary>
        public static IInputManager Instance => PerspexLocator.Current.GetService<IInputManager>();

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PreProcess => _preProcess;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> Process => _process;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PostProcess => _postProcess;

        /// <inheritdoc/>
        public void ProcessInput(RawInputEventArgs e)
        {
            _process.OnNext(e);
            _postProcess.OnNext(e);
        }
    }
}
