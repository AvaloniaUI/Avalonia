using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Holds a registry of plugins used for bindings.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
    public static class BindingPlugins
    {
        internal static readonly List<IPropertyAccessorPlugin> s_propertyAccessors = new()
        {
            new AvaloniaPropertyAccessorPlugin(),
            new ReflectionMethodAccessorPlugin(),
            new InpcPropertyAccessorPlugin(),
        };

        internal static readonly List<IDataValidationPlugin> s_dataValidators = new()
        {
            new DataAnnotationsValidationPlugin(),
            new IndeiValidationPlugin(),
            new ExceptionValidationPlugin(),
        };

        internal static readonly List<IStreamPlugin> s_streamHandlers = new()
        {
            new TaskStreamPlugin(),
            new ObservableStreamPlugin(),
        };

        /// <summary>
        /// An ordered collection of property accessor plugins that can be used to customize
        /// the reading and subscription of property values on a type.
        /// </summary>
        public static IList<IPropertyAccessorPlugin> PropertyAccessors => s_propertyAccessors;

        /// <summary>
        /// An ordered collection of validation checker plugins that can be used to customize
        /// the validation of view model and model data.
        /// </summary>
        public static IList<IDataValidationPlugin> DataValidators => s_dataValidators;

        /// <summary>
        /// An ordered collection of stream plugins that can be used to customize the behavior
        /// of the '^' stream binding operator.
        /// </summary>
        public static IList<IStreamPlugin> StreamHandlers => s_streamHandlers;
    }
}
