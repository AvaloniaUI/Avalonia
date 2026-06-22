namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that can be set to a value within a range.
    /// </summary>
    public interface IRangeValueProvider
    {
        /// <summary>
        /// Gets a value that indicates whether the value of a control is read-only.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_IsReadOnly</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the minimum range value that is supported by the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_Minimum</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityMinValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        double Minimum { get; }

        /// <summary>
        /// Gets the maximum range value that is supported by the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_Maximum</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityMaxValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        double Maximum { get; }

        /// <summary>
        /// Gets the value of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_Value</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        double Value { get; }

        /// <summary>
        /// Gets the value that is added to or subtracted from the Value property when a large
        /// change is made, such as with the PAGE DOWN key.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_LargeChange</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        double LargeChange { get; }

        /// <summary>
        /// Gets the value that is added to or subtracted from the Value property when a small
        /// change is made, such as with an arrow key.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.get_SmallChange</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       Used by <c>NSAccessibilityProtocol.accessibilityPerformIncrement</c> and
        ///       <c>NSAccessibilityProtocol.accessibilityPerformDecrement</c> to determine the
        ///       changed value.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        double SmallChange { get; }

        /// <summary>
        /// Sets the value of the control.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRangeValueProvider.SetValue</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       <c>NSAccessibilityProtocol.setAccessibilityValue</c>
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public void SetValue(double value);
    }
}
