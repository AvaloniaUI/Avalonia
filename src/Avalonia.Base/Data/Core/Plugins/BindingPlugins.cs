using System.Collections.Generic;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Holds a registry of plugins used for bindings.
    /// </summary>
    public static class BindingPlugins
    {
        /// <summary>
        /// An ordered collection of property accessor plugins that can be used to customize
        /// the reading and subscription of property values on a type.
        /// </summary>
        public static IList<IPropertyAccessorPlugin> PropertyAccessors => ExpressionObserver.PropertyAccessors;

        /// <summary>
        /// An ordered collection of validation checker plugins that can be used to customize
        /// the validation of view model and model data.
        /// </summary>
        public static IList<IDataValidationPlugin> DataValidators => ExpressionObserver.DataValidators;

        /// <summary>
        /// An ordered collection of stream plugins that can be used to customize the behavior
        /// of the '^' stream binding operator.
        /// </summary>
        public static IList<IStreamPlugin> StreamHandlers => ExpressionObserver.StreamHandlers;
    }
}
