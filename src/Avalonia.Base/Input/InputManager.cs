using System;
using System.Reactive.Subjects;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    /// <summary>
    /// Receives input from the windowing subsystem and dispatches it to interested parties
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
        public static IInputManager? Instance => AvaloniaLocator.Current.GetService<IInputManager>();

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PreProcess => _preProcess;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> Process => _process;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PostProcess => _postProcess;

        /// <inheritdoc/>
        public FocusInputDeviceKind LastInputDeviceType { get; private set; }

        /// <inheritdoc/>
        public void ProcessInput(RawInputEventArgs e)
        {
            SetLastInputDeviceType(e);

            _preProcess.OnNext(e);
            e.Device?.ProcessRawEvent(e);
            _process.OnNext(e);
            _postProcess.OnNext(e);
        }

        private void SetLastInputDeviceType(RawInputEventArgs e)
        {
            // FocusInputDeviceKind enum
            // None,            [not valid here]
            // Mouse,           [supported]
            // Touch,           [supported*]
            // Pen,             [not supported yet?]
            // Keyboard,        [supported]
            // GameController   [not supported]

            // RawInputEventArgs types
            //  RawDragEvent -> Ignore, keep existing input type
            //  RawKeyEventArgs -> Keyboard
            //  RawPointerEventArgs -> Mouse or ???
            //   --> RawTouchEventArgs -> Touch
            //  RawTextInputEventArgs -> Keyboard

            if (e is RawKeyEventArgs || e is RawTextInputEventArgs)
            {
                    LastInputDeviceType = FocusInputDeviceKind.Keyboard;
            }
            else if (e is RawTouchEventArgs)
            {
                // TODO: When pen vs. touch is properly supported, make sure to separate here
                LastInputDeviceType = FocusInputDeviceKind.Touch;
            }
            // If doing drag-drop, preserve the last input type, otherwise we mark it as mouse
            else if (!(e is RawDragEvent))
            {
                // TODO: If gamepad/controller/remote support is ever added, separate here
                LastInputDeviceType = FocusInputDeviceKind.Mouse;
            }
        }
    }
}
