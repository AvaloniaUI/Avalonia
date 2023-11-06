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
        public const string Property = nameof(Property);

        /// <summary>
        /// The log event comes from the binding system.
        /// </summary>
        public const string Binding = nameof(Binding);

        /// <summary>
        /// The log event comes from the animations system.
        /// </summary>
        public const string Animations = nameof(Animations);

        /// <summary>
        /// The log event comes from the visual system.
        /// </summary>
        public const string Visual = nameof(Visual);

        /// <summary>
        /// The log event comes from the layout system.
        /// </summary>
        public const string Layout = nameof(Layout);

        /// <summary>
        /// The log event comes from the control system.
        /// </summary>
        public const string Control = nameof(Control);

        /// <summary>
        /// The log event comes from Win32 Platform.
        /// </summary>
        public const string Platform = nameof(Platform);
        
        /// <summary>
        /// The log event comes from Win32 Platform.
        /// </summary>
        public const string Win32Platform = nameof(Win32Platform);
        
        /// <summary>
        /// The log event comes from X11 Platform.
        /// </summary>
        public const string X11Platform = nameof(X11Platform);

        /// <summary>
        /// The log event comes from Android Platform.
        /// </summary>
        public const string AndroidPlatform = nameof(AndroidPlatform);
        
        /// <summary>
        /// The log event comes from iOS Platform.
        /// </summary>
        public const string IOSPlatform = nameof(IOSPlatform);

        /// <summary>
        /// The log event comes from LinuxFramebuffer Platform
        /// </summary>
        public const string LinuxFramebufferPlatform = nameof(LinuxFramebufferPlatform);

        /// <summary>
        /// The log event comes from FreeDesktop Platform
        /// </summary>
        public const string FreeDesktopPlatform = nameof(FreeDesktopPlatform);

        /// <summary>
        /// The log event comes from macOS Platform
        /// </summary>
        public const string macOSPlatform = nameof(macOSPlatform);

        /// <summary>
        /// The log event comes from Browser Platform
        /// </summary>
        public static string BrowserPlatform => nameof(BrowserPlatform);
    }
}
