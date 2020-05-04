using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Holds the details of the <see cref="TemplatedControl.TemplateApplied"/> event.
    /// </summary>
    public class TemplateAppliedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateAppliedEventArgs"/> class.
        /// </summary>
        /// <param name="nameScope">The applied template's name scope.</param>
        public TemplateAppliedEventArgs(INameScope nameScope)
            : base(TemplatedControl.TemplateAppliedEvent)
        {
            NameScope = nameScope;
        }

        /// <summary>
        /// Gets the name scope of the applied template.
        /// </summary>
        public INameScope NameScope { get; }
    }
}
