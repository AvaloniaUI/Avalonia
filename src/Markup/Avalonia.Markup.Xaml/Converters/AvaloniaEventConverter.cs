using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.PortableXaml;
using Portable.Xaml;

namespace Avalonia.Markup.Xaml.Converters
{
    internal class AvaloniaEventConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var text = value as string;
            if (text != null)
            {
                var rootObjectProvider = context.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
                var destinationTypeProvider = context.GetService(typeof(IDestinationTypeProvider)) as IDestinationTypeProvider;
                if (rootObjectProvider != null && destinationTypeProvider != null)
                {
                    var target = rootObjectProvider.RootObject;
                    var eventType = destinationTypeProvider.GetDestinationType();
                    var eventParameters = eventType.GetRuntimeMethods().First(r => r.Name == "Invoke").GetParameters();
                    // go in reverse to match System.Xaml behaviour
                    var methods = target.GetType().GetRuntimeMethods().Reverse();

                    // find based on exact match parameter types first
                    foreach (var method in methods)
                    {
                        if (method.Name != text)
                            continue;
                        var parameters = method.GetParameters();
                        if (eventParameters.Length != parameters.Length)
                            continue;
                        if (parameters.Length == 0)
                            return method.CreateDelegate(eventType, target);

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];
                            var eventParam = eventParameters[i];
                            if (param.ParameterType != eventParam.ParameterType)
                                break;
                            if (i == parameters.Length - 1)
                                return method.CreateDelegate(eventType, target);
                        }
                    }

                    // EnhancedXaml: Find method with compatible base class parameters
                    foreach (var method in methods)
                    {
                        if (method.Name != text)
                            continue;
                        var parameters = method.GetParameters();
                        if (parameters.Length == 0 || eventParameters.Length != parameters.Length)
                            continue;

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];
                            var eventParam = eventParameters[i];
                            if (!param.ParameterType.GetTypeInfo().IsAssignableFrom(eventParam.ParameterType.GetTypeInfo()))
                                break;
                            if (i == parameters.Length - 1)
                                return method.CreateDelegate(eventType, target);
                        }
                    }

                    var contextProvider = (IXamlSchemaContextProvider)context.GetService(typeof(IXamlSchemaContextProvider));
                    var avaloniaContext = (AvaloniaXamlSchemaContext)contextProvider.SchemaContext;

                    if (avaloniaContext.IsDesignMode)
                    {
                        // We want to ignore missing events in the designer, so if event handler
                        // wasn't found create an empty delegate.
                        var lambdaExpression = Expression.Lambda(
                            eventType,
                            Expression.Empty(),
                            eventParameters.Select(x => Expression.Parameter(x.ParameterType)));
                        return lambdaExpression.Compile();
                    }
                    else
                    {
                        throw new XamlObjectWriterException($"Referenced value method {text} in type {target.GetType()} indicated by event {eventType.FullName} was not found");
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
