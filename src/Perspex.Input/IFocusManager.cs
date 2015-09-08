





namespace Perspex.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    public interface IFocusManager
    {
        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        IInputElement Current { get; }

        /// <summary>
        /// Gets the current focus scope.
        /// </summary>
        IFocusScope Scope { get; }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        void Focus(IInputElement control, NavigationMethod method = NavigationMethod.Unspecified);

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        /// <remarks>
        /// This should not be called by client code. It is called by an <see cref="IFocusScope"/>
        /// when it activates, e.g. when a Window is activated.
        /// </remarks>
        void SetFocusScope(IFocusScope scope);
    }
}
