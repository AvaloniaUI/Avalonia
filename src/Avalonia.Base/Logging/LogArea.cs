namespace Avalonia.Logging
{
    /// <summary>
    /// Specifies the area in which a log event occurred.
    /// </summary>
    public static class LogArea
    {
        /// <summary>
        /// The log event comes from the property system.
        /// </summary>
        public const string Property = "Property";

        /// <summary>
        /// The log event comes from the binding system.
        /// </summary>
        public const string Binding = "Binding";

        /// <summary>
        /// The log event comes from the animations system.
        /// </summary>
        public const string Animations = "Animations";

        /// <summary>
        /// The log event comes from the visual system.
        /// </summary>
        public const string Visual = "Visual";

        /// <summary>
        /// The log event comes from the layout system.
        /// </summary>
        public const string Layout = "Layout";

        /// <summary>
        /// The log event comes from the control system.
        /// </summary>
        public const string Control = "Control";
    }
}
