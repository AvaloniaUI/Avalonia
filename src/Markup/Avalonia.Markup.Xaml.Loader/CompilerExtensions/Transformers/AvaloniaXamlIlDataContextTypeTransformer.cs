﻿using System;
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
                AvaloniaXamlIlDataContextTypeMetadataNode inferredDataContextTypeNode = null;
                AvaloniaXamlIlDataContextTypeMetadataNode directiveDataContextTypeNode = null;

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
                            if (directive.Values[0] is XamlAstTextNode text)
                            {
                                directiveDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on,
                                    TypeReferenceResolver.ResolveType(context, text.Text, isMarkupExtension: false, text, strict: true).Type);
                            }
                            else
                            {
                                throw new XamlX.XamlParseException("x:DataType should be set to a type name.", directive.Values[0]);
                            }
                        }
                    }
                    else if (child is XamlPropertyAssignmentNode pa)
                    {
                        if (pa.Property.Name == "DataContext"
                            && pa.Property.DeclaringType.Equals(context.GetAvaloniaTypes().StyledElement)
                            && pa.Values[0] is XamlMarkupExtensionNode ext
                            && ext.Value is XamlAstConstructableObjectNode obj)
                        {
                            inferredDataContextTypeNode = ParseDataContext(context, on, obj);
                        }
                        else if(context.GetAvaloniaTypes().DataTemplate.IsAssignableFrom(on.Type.GetClrType())
                            && pa.Property.Name == "DataType"
                            && pa.Values[0] is XamlTypeExtensionNode dataTypeNode)
                        {
                            inferredDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, dataTypeNode.Value.GetClrType());
                        }
                    }
                }

                // If there is no x:DataType directive,
                // do more specialized inference
                if (directiveDataContextTypeNode is null)
                {
                    if (context.GetAvaloniaTypes().IDataTemplate.IsAssignableFrom(on.Type.GetClrType())
                        && inferredDataContextTypeNode is null)
                    {
                        // Infer data type from collection binding on a control that displays items.
                        var parentObject = context.ParentNodes().OfType<XamlAstConstructableObjectNode>().FirstOrDefault();
                        if (parentObject != null)
                        {
                            var parentType = parentObject.Type.GetClrType();

                            if (context.GetAvaloniaTypes().IItemsPresenterHost.IsDirectlyAssignableFrom(parentType)
                                || context.GetAvaloniaTypes().ItemsRepeater.IsDirectlyAssignableFrom(parentType))
                            {
                                inferredDataContextTypeNode = InferDataContextOfPresentedItem(context, on, parentObject);
                            }
                        }

                        if (inferredDataContextTypeNode is null)
                        {
                            inferredDataContextTypeNode = new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
                        }
                    }
                }

                return directiveDataContextTypeNode ?? inferredDataContextTypeNode ?? node;
            }

            return node;
        }

        private static AvaloniaXamlIlDataContextTypeMetadataNode InferDataContextOfPresentedItem(AstTransformationContext context, XamlAstConstructableObjectNode on, XamlAstConstructableObjectNode parentObject)
        {
            var parentItemsValue = parentObject
                                            .Children.OfType<XamlPropertyAssignmentNode>()
                                            .FirstOrDefault(pa => pa.Property.Name == "Items")
                                            ?.Values[0];
            if (parentItemsValue is null)
            {
                // We can't infer the collection type and the currently calculated type is definitely wrong.
                // Notify the user that we were unable to infer the data context type if they use a compiled binding.
                return new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
            }

            IXamlType itemsCollectionType = null;
            if (context.GetAvaloniaTypes().IBinding.IsAssignableFrom(parentItemsValue.Type.GetClrType()))
            {
                if (parentItemsValue.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension)
                    && parentItemsValue is XamlMarkupExtensionNode ext && ext.Value is XamlAstConstructableObjectNode parentItemsBinding)
                {
                    var parentItemsDataContext = context.ParentNodes().SkipWhile(n => n != parentObject).OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentItemsDataContext != null)
                    {
                        itemsCollectionType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, parentItemsBinding, () => parentItemsDataContext.DataContextType, parentObject.Type.GetClrType());
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
            // We can't infer the collection type and the currently calculated type is definitely wrong.
            // Notify the user that we were unable to infer the data context type if they use a compiled binding.
            return new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
        }

        private static AvaloniaXamlIlDataContextTypeMetadataNode ParseDataContext(AstTransformationContext context, XamlAstConstructableObjectNode on, XamlAstConstructableObjectNode obj)
        {
            var bindingType = context.GetAvaloniaTypes().IBinding;
            if (!bindingType.IsAssignableFrom(obj.Type.GetClrType()) && !obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().ReflectionBindingExtension))
            {
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, obj.Type.GetClrType());
            }
            else if (obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                Func<IXamlType> startTypeResolver = () =>
                {
                    var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentDataContextNode is null)
                    {
                        throw new XamlX.XamlParseException("Cannot parse a compiled binding without an explicit x:DataType directive to give a starting data type for bindings.", obj);
                    }

                    return parentDataContextNode.DataContextType;
                };

                var bindingResultType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, obj, startTypeResolver, on.Type.GetClrType());
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
        public virtual IXamlType DataContextType { get; }

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
            : base(value, null)
        {
        }

        public override IXamlType DataContextType => throw new XamlTransformException("Unable to infer DataContext type for compiled bindings nested within this element.", Value);
    }
}
