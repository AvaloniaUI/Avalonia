using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Base class for avalonia property metadata.
    /// </summary>
    public class AvaloniaPropertyMetadata
    {
        private BindingMode _defaultBindingMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        public AvaloniaPropertyMetadata(
            BindingMode defaultBindingMode = BindingMode.Default)
        {
            _defaultBindingMode = defaultBindingMode;
        }

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        public BindingMode DefaultBindingMode
        {
            get
            {
                return _defaultBindingMode == BindingMode.Default ?
                    BindingMode.OneWay : _defaultBindingMode;
            }
        }

        /// <summary>
        /// Merges the metadata with the base metadata.
        /// </summary>
        /// <param name="baseMetadata">The base metadata to merge.</param>
        /// <param name="property">The property to which the metadata is being applied.</param>
        public virtual void Merge(
            AvaloniaPropertyMetadata baseMetadata, 
            AvaloniaProperty property)
        {
            if (_defaultBindingMode == BindingMode.Default)
            {
                _defaultBindingMode = baseMetadata.DefaultBindingMode;
            }
        }
    }
}
