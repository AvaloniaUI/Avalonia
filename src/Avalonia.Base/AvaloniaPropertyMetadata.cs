using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Base class for avalonia property metadata.
    /// </summary>
    public class AvaloniaPropertyMetadata
    {
        private BindingMode _defaultBindingMode;
        private UpdateSourceTrigger _defaultUpdateSourceTrigger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="defaultUpdateSourceTrigger"></param>
        public AvaloniaPropertyMetadata(
            BindingMode defaultBindingMode = BindingMode.Default,
            UpdateSourceTrigger defaultUpdateSourceTrigger = UpdateSourceTrigger.Default)
        {
            _defaultBindingMode = defaultBindingMode;
            _defaultUpdateSourceTrigger = defaultUpdateSourceTrigger;
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
        /// 
        /// </summary>
        public UpdateSourceTrigger DefaultUpdateSourceTrigger
        {
            get
            {
                return _defaultUpdateSourceTrigger == UpdateSourceTrigger.Default ?
                    UpdateSourceTrigger.PropertyChanged : _defaultUpdateSourceTrigger;
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

            if (_defaultUpdateSourceTrigger == UpdateSourceTrigger.Default)
            {
                _defaultUpdateSourceTrigger = baseMetadata.DefaultUpdateSourceTrigger;
            }
        }
    }
}
