using Avalonia.Automation.Provider;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="IToggleProvider"/>.
    /// </summary>
    public static class TogglePatternIdentifiers
    {
        /// <summary>
        /// Identifies the <see cref="IToggleProvider.ToggleState"/> property.
        /// </summary>
        public static AutomationProperty ToggleStateProperty { get; } = new AutomationProperty();
    }
}
