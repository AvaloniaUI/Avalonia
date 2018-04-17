namespace Avalonia.Controls {

    /// <summary>
    /// User control that involve in navigation with history.
    /// </summary>
    public class Page : UserControl
    {

        /// <summary>
        /// Frame that page belongs to.
        /// </summary>
        public Frame Frame { get; set; }

        /// <summary>
        /// Invoked when the page leave the frame.
        /// </summary>
        /// <param name="eventArguments">Class contains data related with navigation.</param>
        public virtual void NavigateFrom ( NavigationEventArgs eventArguments )
        {
        }

        /// <summary>
        /// Invoked after load this the page in the frame.
        /// </summary>
        /// <param name="eventArguments">Class contains data related with navigation.</param>
        public virtual void NavigateTo ( NavigationEventArgs eventArguments )
        {
        }

    }

}