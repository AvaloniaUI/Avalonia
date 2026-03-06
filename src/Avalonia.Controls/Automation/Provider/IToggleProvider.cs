namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Contains values that specify the toggle state of a UI Automation element.
    /// </summary>
    public enum ToggleState
    {
        /// <summary>
        /// The UI Automation element isn't selected, checked, marked, or otherwise activated.
        /// </summary>
        Off,

        /// <summary>
        /// The UI Automation element is selected, checked, marked, or otherwise activated.
        /// </summary>
        On,

        /// <summary>
        /// The UI Automation element is in an indeterminate state.
        /// </summary>
        Indeterminate,
    }

    /// <summary>
    /// Exposes methods and properties to support UI Automation client access to controls that can
    /// cycle through a set of states and maintain a particular state. 
    /// </summary>
    public interface IToggleProvider
    {
        /// <summary>
        /// Gets the toggle state of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IToggleProvider.ToggleState</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        ToggleState ToggleState { get; }

        /// <summary>
        /// Cycles through the toggle states of a control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IToggleProvider.Toggle</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityPerformPress</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        void Toggle();
    }
}
