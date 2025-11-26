using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class XamlDataContextException : XamlTransformException
    {
        public XamlDataContextException(string message, IXamlLineInfo lineInfo, Exception? innerException = null)
            : base(message, lineInfo, innerException)
        {
        }
    }

    class AvaloniaXamlIlDataContextTypeTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlDataContextTypeMetadataNode)
            {
                // We've already resolved the data context type for this node.
                return node;
            }

            if (node is XamlAstConstructableObjectNode on)
            {
                AvaloniaXamlIlDataContextTypeMetadataNode? inferredDataContextTypeNode = null;
                AvaloniaXamlIlDataContextTypeMetadataNode? directiveDataContextTypeNode = null;

                for (int i = 0; i < on.Children.Count; ++i)
                {
                    var child = on.Children[i];
                    if (child is XamlAstXmlDirective directive)
                    {
                        if (directive.Namespace == XamlNamespaces.Xaml2006
                            && directive.Name == "DataType"
                            && directive.Values.Count == 1)
                        {
                            on.Children.RemoveAt(i);
                            i--;
                            if (directive.Values[0] is XamlTypeExtensionNode typeNode)
                            {
                                directiveDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, typeNode.Value.GetClrType());
                            }
                            else if (directive.Values[0] is XamlAstTextNode text)
                            {
                                directiveDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on,
                                    TypeReferenceResolver.ResolveType(context, text.Text, isMarkupExtension: false, text, strict: true).Type);
                            }
                            else
                            {
                                throw new XamlDataContextException("x:DataType should be set to a type name.", directive.Values[0]);
                            }
                        }
                    }
                    else if (child is XamlPropertyAssignmentNode pa)
                    {
                        var templateDataTypeAttribute = context.GetAvaloniaTypes().DataTypeAttribute;
                        
                        if (pa.Property.Name == "DataContext"
                            && pa.Property.DeclaringType.Equals(context.GetAvaloniaTypes().StyledElement)
                            && pa.Values[0] is XamlMarkupExtensionNode ext
                            && ext.Value is XamlAstConstructableObjectNode obj)
                        {
                            inferredDataContextTypeNode = ParseDataContext(context, on, obj);
                        }
                        else if(pa.Property.CustomAttributes.Any(a => a.Type == templateDataTypeAttribute)
                            && pa.Values[0] is XamlTypeExtensionNode dataTypeNode)
                        {
                            inferredDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, dataTypeNode.Value.GetClrType());
                        }
                    }
                }

                // If there is no x:DataType directive,
                // do more specialized inference
                if (directiveDataContextTypeNode is null && inferredDataContextTypeNode is null)
                {
                    // Infer data type from collection binding on a control that displays items.
                    var property = context.ParentNodes().OfType<XamlPropertyAssignmentNode>().FirstOrDefault();
                    var attributeType = context.GetAvaloniaTypes().InheritDataTypeFromItemsAttribute;
                    var attribute = property?.Property.GetClrProperty().CustomAttributes
                        .FirstOrDefault(a => a.Type == attributeType);
    
                    if (attribute is not null)
                    {
                        var propertyName = (string?)attribute.Parameters.First();
                        XamlAstConstructableObjectNode? parentObject;
                        if (attribute.Properties.TryGetValue("AncestorType", out var type)
                            && type is IXamlType xamlType)
                        {
                            parentObject = context.ParentNodes().OfType<XamlAstConstructableObjectNode>()
                                .FirstOrDefault(n => xamlType.IsAssignableFrom(n.Type.GetClrType()));
                        }
                        else
                        {
                            parentObject = context.ParentNodes().OfType<XamlAstConstructableObjectNode>().FirstOrDefault();
                        }
                            
                        if (parentObject != null && !string.IsNullOrEmpty(propertyName))
                        {
                            inferredDataContextTypeNode = InferDataContextOfPresentedItem(context, on, parentObject, propertyName);
                        }
                    }
                    
                    if (inferredDataContextTypeNode is null
                        // Only for IDataTemplate, as we want to notify user as early as possible,
                        // and IDataTemplate cannot inherit DataType from the parent implicitly.
                        && context.GetAvaloniaTypes().IDataTemplate.IsAssignableFrom(on.Type.GetClrType()))
                    {
                        // We can't infer the collection type and the currently calculated type is definitely wrong.
                        // Notify the user that we were unable to infer the data context type if they use a compiled binding.
                        inferredDataContextTypeNode = new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
                    }
                }

                return directiveDataContextTypeNode ?? inferredDataContextTypeNode ?? node;
            }

            return node;
        }
        
        private static AvaloniaXamlIlDataContextTypeMetadataNode? InferDataContextOfPresentedItem(
            AstTransformationContext context, XamlAstConstructableObjectNode on,
            XamlAstConstructableObjectNode parentObject, string propertyName)
        {
            var parentItemsValue = parentObject
                                            .Children.OfType<XamlPropertyAssignmentNode>()
                                            .FirstOrDefault(pa => pa.Property.Name == propertyName)
                                            ?.Values[0];
            if (parentItemsValue is null)
            {
                return null;
            }

            IXamlType? itemsCollectionType = null;
            if (context.GetAvaloniaTypes().BindingBase.IsAssignableFrom(parentItemsValue.Type.GetClrType()))
            {
                if (parentItemsValue.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBinding)
                    && parentItemsValue is XamlMarkupExtensionNode ext && ext.Value is XamlAstConstructableObjectNode parentItemsBinding)
                {
                    var parentItemsDataContext = context.ParentNodes().SkipWhile(n => n != parentObject).OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentItemsDataContext != null)
                    {
                        itemsCollectionType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context,
                            parentItemsBinding, () => parentItemsDataContext.DataContextType,
                            parentObject.Type.GetClrType());
                    }
                }
            }
            else
            {
                itemsCollectionType = parentItemsValue.Type.GetClrType();
            }

            if (itemsCollectionType != null)
            {
                foreach (var i in GetAllInterfacesIncludingSelf(itemsCollectionType))
                {
                    if (i.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes.IEnumerableT) == true)
                    {
                        return new AvaloniaXamlIlDataContextTypeMetadataNode(on, i.GenericArguments[0]);
                    }
                }
            }
            
            return null;
        } 

        private static AvaloniaXamlIlDataContextTypeMetadataNode ParseDataContext(AstTransformationContext context, XamlAstConstructableObjectNode on, XamlAstConstructableObjectNode obj)
        {
            var bindingType = context.GetAvaloniaTypes().BindingBase;
            if (!bindingType.IsAssignableFrom(obj.Type.GetClrType()) && 
                !(obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().ReflectionBindingExtension) ||
                  obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension)))
            {
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, obj.Type.GetClrType());
            }
            else if (obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                Func<IXamlType> startTypeResolver = () =>
                {
                    var dataTypeProperty = obj.Children.OfType<XamlPropertyAssignmentNode>().FirstOrDefault(c => c.Property.Name == "DataType");
                    if (dataTypeProperty?.Values.Count is 1 && dataTypeProperty.Values[0] is XamlAstTextNode text)
                    {
                        return TypeReferenceResolver.ResolveType(context, text.Text, isMarkupExtension: false, text, strict: true).Type;
                    }

                    var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentDataContextNode is null)
                    {
                        throw new XamlDataContextException("Cannot parse a compiled binding without an explicit x:DataType directive to give a starting data type for bindings.", obj);
                    }

                    return parentDataContextNode.DataContextType;
                };

                var bindingResultType =
                    XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, obj, startTypeResolver,
                        on.Type.GetClrType());
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, bindingResultType);
            }

            return new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
        }

        private static IEnumerable<IXamlType> GetAllInterfacesIncludingSelf(IXamlType type)
        {
            if (type.IsInterface)
                yield return type;

            foreach (var i in type.GetAllInterfaces())
                yield return i;
        }
    }

    [DebuggerDisplay("DataType = {DataContextType}")]
    class AvaloniaXamlIlDataContextTypeMetadataNode : XamlValueWithSideEffectNodeBase
    {
        public IXamlType DataContextType { get; }

        public AvaloniaXamlIlDataContextTypeMetadataNode(IXamlAstValueNode value, IXamlType targetType)
            : base(value, value)
        {
            DataContextType = targetType;
        }
    }

    [DebuggerDisplay("DataType = Unknown")]
    class AvaloniaXamlIlUninferrableDataContextMetadataNode : AvaloniaXamlIlDataContextTypeMetadataNode
    {
        public AvaloniaXamlIlUninferrableDataContextMetadataNode(IXamlAstValueNode value)
            : base(value, XamlPseudoType.Unknown)
        {
        }
    }
}
