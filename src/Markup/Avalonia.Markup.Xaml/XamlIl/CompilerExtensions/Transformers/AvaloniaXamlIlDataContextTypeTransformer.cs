using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Utilities;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlDataContextTypeTransformer : IXamlIlAstTransformer
    {
        private const string AvaloniaNs = "https://github.com/avaloniaui";
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (context.ParentNodes().FirstOrDefault() is AvaloniaXamlIlDataContextTypeMetadataNode)
            {
                // We've already resolved the data context type for this node.
                return node;
            }

            if (node is XamlIlAstObjectNode on)
            {
                AvaloniaXamlIlDataContextTypeMetadataNode inferredDataContextTypeNode = null;
                AvaloniaXamlIlDataContextTypeMetadataNode directiveDataContextTypeNode = null;
                bool isDataTemplate = on.Type.GetClrType().Equals(context.GetAvaloniaTypes().DataTemplate);

                for (int i = 0; i < on.Children.Count; ++i)
                {
                    var child = on.Children[i];
                    if (child is XamlIlAstXmlDirective directive)
                    {
                        if (directive.Namespace == XamlNamespaces.Xaml2006
                            && directive.Name == "DataContextType"
                            && directive.Values.Count == 1)
                        {
                            on.Children.RemoveAt(i);
                            i--;
                            if (directive.Values[0] is XamlIlAstTextNode text)
                            {
                                directiveDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on,
                                    XamlIlTypeReferenceResolver.ResolveType(context, text.Text, isMarkupExtension: false, text, strict: true).Type);
                            }
                            else
                            {
                                throw new XamlIlParseException("x:DataContextType should be set to a type name.", directive.Values[0]);
                            }
                        }
                    }
                    else if (child is XamlIlPropertyAssignmentNode pa)
                    {
                        if (pa.Property.Name == "DataContext"
                            && pa.Property.DeclaringType.Equals(context.GetAvaloniaTypes().StyledElement)
                            && pa.Values[0] is XamlIlMarkupExtensionNode ext
                            && ext.Value is XamlIlAstObjectNode obj)
                        {
                            inferredDataContextTypeNode = ParseDataContext(context, on, obj);
                        }
                        else if(isDataTemplate
                            && pa.Property.Name == "DataType"
                            && pa.Values[0] is XamlIlTypeExtensionNode dataTypeNode)
                        {
                            inferredDataContextTypeNode = new AvaloniaXamlIlDataContextTypeMetadataNode(on, dataTypeNode.Value.GetClrType());
                        }
                    }
                }

                // If there is no x:DataContextType directive,
                // do more specialized inference
                if (directiveDataContextTypeNode is null)
                {
                    if (isDataTemplate && inferredDataContextTypeNode is null)
                    {
                        // Infer data type from collection binding on a control that displays items.
                        var parentObject = context.ParentNodes().OfType<XamlIlAstObjectNode>().FirstOrDefault();
                        if (parentObject != null && context.GetAvaloniaTypes().IItemsPresenterHost.IsDirectlyAssignableFrom(parentObject.Type.GetClrType()))
                        {
                            inferredDataContextTypeNode = InferDataContextOfPresentedItem(context, on, parentObject);
                        }
                        else
                        {
                            inferredDataContextTypeNode = new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
                        }
                    }
                }

                return directiveDataContextTypeNode ?? inferredDataContextTypeNode ?? node;
            }

            return node;
        }

        private static AvaloniaXamlIlDataContextTypeMetadataNode InferDataContextOfPresentedItem(XamlIlAstTransformationContext context, XamlIlAstObjectNode on, XamlIlAstObjectNode parentObject)
        {
            var parentItemsValue = parentObject
                                            .Children.OfType<XamlIlPropertyAssignmentNode>()
                                            .FirstOrDefault(pa => pa.Property.Name == "Items")
                                            ?.Values[0];
            if (parentItemsValue is null)
            {
                // We can't infer the collection type and the currently calculated type is definitely wrong.
                // Notify the user that we were unable to infer the data context type if they use a compiled binding.
                return new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
            }

            IXamlIlType itemsCollectionType = null;
            if (context.GetAvaloniaTypes().IBinding.IsAssignableFrom(parentItemsValue.Type.GetClrType()))
            {
                if (parentItemsValue.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension)
                    && parentItemsValue is XamlIlMarkupExtensionNode ext && ext.Value is XamlIlAstObjectNode parentItemsBinding)
                {
                    var parentItemsDataContext = context.ParentNodes().SkipWhile(n => n != parentObject).OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentItemsDataContext != null)
                    {
                        itemsCollectionType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, parentItemsBinding, parentItemsDataContext.DataContextType);
                    }
                }
            }
            else
            {
                itemsCollectionType = parentItemsValue.Type.GetClrType();
            }

            if (itemsCollectionType != null)
            {
                var elementType = itemsCollectionType
                    .GetAllInterfaces()
                    .FirstOrDefault(i =>
                        i.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes.IEnumerableT) == true)
                    .GenericArguments[0];
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, elementType);
            }
            // We can't infer the collection type and the currently calculated type is definitely wrong.
            // Notify the user that we were unable to infer the data context type if they use a compiled binding.
            return new AvaloniaXamlIlUninferrableDataContextMetadataNode(on);
        }

        private static AvaloniaXamlIlDataContextTypeMetadataNode ParseDataContext(XamlIlAstTransformationContext context, XamlIlAstObjectNode on, XamlIlAstObjectNode obj)
        {
            var bindingType = context.GetAvaloniaTypes().IBinding;
            if (!bindingType.IsAssignableFrom(obj.Type.GetClrType()))
            {
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, obj.Type.GetClrType());
            }
            else if (obj.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                IXamlIlType startType;
                var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                if (parentDataContextNode is null)
                {
                    throw new XamlIlParseException("Cannot parse a compiled binding without an explicit x:DataContextType directive to give a starting data type for bindings.", obj);
                }

                startType = parentDataContextNode.DataContextType;

                var bindingResultType = XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, obj, startType);
                return new AvaloniaXamlIlDataContextTypeMetadataNode(on, bindingResultType);
            }

            return null;
        }
    }

    class AvaloniaXamlIlDataContextTypeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public virtual IXamlIlType DataContextType { get; }

        public AvaloniaXamlIlDataContextTypeMetadataNode(IXamlIlAstValueNode value, IXamlIlType targetType)
            : base(value, value)
        {
            DataContextType = targetType;
        }
    }

    class AvaloniaXamlIlUninferrableDataContextMetadataNode : AvaloniaXamlIlDataContextTypeMetadataNode
    {
        public AvaloniaXamlIlUninferrableDataContextMetadataNode(IXamlIlAstValueNode value)
            : base(value, null)
        {
        }

        public override IXamlIlType DataContextType => throw new XamlIlTransformException("Unable to infer DataContext type for compiled bindings nested within this element.", Value);
    }
}
