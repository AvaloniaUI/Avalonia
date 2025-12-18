namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that have an intrinsic value that does not span a range and that can be represented as
    /// a string.
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>
        /// Gets a value that indicates whether the value of a control is read-only.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IValueProvider.IsReadOnly</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the value of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IValueProvider.Value</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string? Value { get; }

        /// <summary>
        /// Sets the value of a control.
        /// </summary>
        /// <param name="value">
        /// The value to set. The provider is responsible for converting the value to the
        /// appropriate data type.
        /// </param>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IValueProvider.SetValue</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       <c>NSAccessibilityProtocol.setAccessibilityValue</c>
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public void SetValue(string? value);
    }
}
