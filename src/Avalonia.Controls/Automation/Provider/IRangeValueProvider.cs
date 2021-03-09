#nullable enable

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
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the minimum range value that is supported by the control.
        /// </summary>
        double Minimum { get; }

        /// <summary>
        /// Gets the maximum range value that is supported by the control.
        /// </summary>
        double Maximum { get; }

        /// <summary>
        /// Gets the value of the control.
        /// </summary>
        double Value { get; }

        /// <summary>
        /// Sets the value of the control.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetValue(double value);
    }
}
