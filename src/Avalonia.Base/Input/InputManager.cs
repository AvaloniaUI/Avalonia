using System;
using Avalonia.Input.Raw;
using Avalonia.Reactive;

namespace Avalonia.Input
{
    /// <summary>
    /// Receives input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    internal class InputManager : IInputManager
    {
        private readonly LightweightSubject<RawInputEventArgs> _preProcess = new();
        private readonly LightweightSubject<RawInputEventArgs> _process = new();
        private readonly LightweightSubject<RawInputEventArgs> _postProcess = new();

        /// <summary>
        /// Gets the global instance of the input manager.
        /// </summary>
        public static IInputManager? Instance => AvaloniaLocator.Current.GetService<IInputManager>();

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PreProcess => _preProcess;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> Process => _process;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PostProcess => _postProcess;

        /// <inheritdoc/>
        public void ProcessInput(RawInputEventArgs e)
        {
            _preProcess.OnNext(e);
            e.Device?.ProcessRawEvent(e);
            _process.OnNext(e);
            _postProcess.OnNext(e);
        }
    }
}
