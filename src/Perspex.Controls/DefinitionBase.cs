





namespace Perspex.Controls
{
    /// <summary>
    /// Base class for <see cref="ColumnDefinition"/> and <see cref="RowDefinition"/>.
    /// </summary>
    public class DefinitionBase : PerspexObject
    {
        /// <summary>
        /// Defines the <see cref="SharedSizeGroup"/> property.
        /// </summary>
        public static readonly PerspexProperty<string> SharedSizeGroupProperty =
            PerspexProperty.Register<DefinitionBase, string>(nameof(SharedSizeGroup), inherits: true);

        /// <summary>
        /// Gets or sets the name of the shared size group of the column or row.
        /// </summary>
        public string SharedSizeGroup
        {
            get { return this.GetValue(SharedSizeGroupProperty); }
            set { this.SetValue(SharedSizeGroupProperty, value); }
        }
    }
}