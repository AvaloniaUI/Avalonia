namespace Avalonia.Input
{
    /// <summary>
    /// Specifies input device types from which input events are received
    /// </summary>
    public enum FocusInputDeviceKind
    {
        /// <summary>
        /// No input
        /// </summary>
        None,

        /// <summary>
        /// Mouse input device
        /// </summary>
        Mouse,

        /// <summary>
        /// Touch input device
        /// </summary>
        Touch,

        /// <summary>
        /// Pen input device
        /// </summary>
        Pen,

        /// <summary>
        /// Keyboard input device
        /// </summary>
        Keyboard,

        /// <summary>
        /// Controller/remote input device, currently unused
        /// </summary>
        Controller
    }
}
