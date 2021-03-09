namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support UI Automation client access to controls that
    /// visually expand to display content and collapse to hide content.
    /// </summary>
    public interface IExpandCollapseProvider
    {
        /// <summary>
        /// Gets the state, expanded or collapsed, of the control.
        /// </summary>
        ExpandCollapseState ExpandCollapseState { get; }

        /// <summary>
        /// Displays all child nodes, controls, or content of the control.
        /// </summary>
        void Expand();

        /// <summary>
        /// Hides all nodes, controls, or content that are descendants of the control.
        /// </summary>
        void Collapse();
    }
}
