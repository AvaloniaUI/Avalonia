





namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    public interface IStyleable : IObservablePropertyBag, INamed
    {
        /// <summary>
        /// Gets the list of classes for the control.
        /// </summary>
        Classes Classes { get; }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        Type StyleKey { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        ITemplatedControl TemplatedParent { get; }
    }
}
