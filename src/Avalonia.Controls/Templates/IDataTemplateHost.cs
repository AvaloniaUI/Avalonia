using Avalonia.Metadata;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Defines an element that has a <see cref="DataTemplates"/> collection.
    /// </summary>
    [NotClientImplementable]
    public interface IDataTemplateHost
    {
        /// <summary>
        /// Gets the data templates for the element.
        /// </summary>
        DataTemplates DataTemplates { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="DataTemplates"/> is initialized.
        /// </summary>
        /// <remarks>
        /// The <see cref="DataTemplates"/> property may be lazily initialized, if so this property
        /// indicates whether it has been initialized.
        /// </remarks>
        bool IsDataTemplatesInitialized { get; }
    }
}
