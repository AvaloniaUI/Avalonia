using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Base class for avalonia property metadata.
    /// </summary>
    public abstract class AvaloniaPropertyMetadata
    {
        private BindingMode _defaultBindingMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="enableDataValidation">Whether the property is interested in data validation.</param>
        public AvaloniaPropertyMetadata(
            BindingMode defaultBindingMode = BindingMode.Default,
            bool? enableDataValidation = null)
        {
            _defaultBindingMode = defaultBindingMode;
            EnableDataValidation = enableDataValidation;
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
        /// Gets a value indicating whether the property is interested in data validation.
        /// </summary>
        /// <remarks>
        /// Data validation is validation performed at the target of a binding, for example in a
        /// view model using the INotifyDataErrorInfo interface. Only certain properties on a
        /// control (such as a TextBox's Text property) will be interested in receiving data
        /// validation messages so this feature must be explicitly enabled by setting this flag.
        /// </remarks>
        public bool? EnableDataValidation { get; private set; }

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

            EnableDataValidation ??= baseMetadata.EnableDataValidation;
        }

        /// <summary>
        /// Gets a copy of this object configured for use with any owner type.
        /// </summary>
        /// <remarks>
        /// For example, delegates which receive the owner object should be removed.
        /// </remarks>
        public abstract AvaloniaPropertyMetadata GenerateTypeSafeMetadata();
    }
}
