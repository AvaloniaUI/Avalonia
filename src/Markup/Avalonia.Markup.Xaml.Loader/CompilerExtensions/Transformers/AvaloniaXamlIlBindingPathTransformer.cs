using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstConstructableObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                IXamlType startType = null;
                var sourceProperty = binding.Children.OfType<XamlPropertyAssignmentNode>().FirstOrDefault(c => c.Property.Name == "Source");
                var dataTypeProperty = binding.Children.OfType<XamlPropertyAssignmentNode>().FirstOrDefault(c => c.Property.Name == "DataType");
                if (sourceProperty?.Values.Count is 1)
                {
                    var sourceValue = sourceProperty.Values[0];
                    switch (sourceValue)
                    {
                        case XamlAstTextNode textNode:
                            startType = textNode.Type?.GetClrType();
                            break;

                        case XamlMarkupExtensionNode extension:
                            startType = extension.Type?.GetClrType();

                            //let's try to infer StaticResource type from parent resources in xaml
                            if (extension.Value.Type.GetClrType().FullName == "Avalonia.Markup.Xaml.MarkupExtensions.StaticResourceExtension" &&
                                extension.Value is XamlAstConstructableObjectNode cn &&
                                cn.Arguments.Count == 1 && cn.Arguments[0] is XamlAstTextNode keyNode)
                            {
                                bool matchProperty(IXamlAstNode node, IXamlType styledElementType, string propertyName)
                                {
                                    return (node is XamlPropertyAssignmentNode p &&
                                            p.Property.DeclaringType == styledElementType && p.Property.Name == propertyName)
                                           ||
                                           (node is XamlManipulationGroupNode m && m.Children.Count > 0 &&
                                            m.Children[0] is XamlPropertyAssignmentNode pm &&
                                            pm.Property.DeclaringType == styledElementType && pm.Property.Name == propertyName);
                                }

                                string getResourceValue_xKey(XamlPropertyAssignmentNode node)
                                    => node.Values.Count == 2 && node.Values[0] is XamlAstTextNode t ? t.Text : "";

                                IXamlType getResourceValue_Type(XamlPropertyAssignmentNode node, IXamlType xamlType)
                                    => node.Values.Count == 2 ? node.Values[1].Type.GetClrType() : xamlType;

                                IEnumerable<XamlPropertyAssignmentNode> getResourceValues(IXamlAstNode node)
                                {
                                    if (node is XamlPropertyAssignmentNode propertyNode)
                                    {
                                        if (propertyNode.Values.Count == 1 &&
                                            propertyNode.Values[0] is XamlAstConstructableObjectNode obj &&
                                            obj.Type.GetClrType().FullName == "Avalonia.Controls.ResourceDictionary")
                                        {
                                            foreach (var r in obj.Children.SelectMany(c => getResourceValues(c)))
                                            {
                                                yield return r;
                                            }
                                        }
                                        else
                                        {
                                            yield return propertyNode;
                                        }
                                    }
                                    else if (node is XamlManipulationGroupNode m)
                                    {
                                        foreach (var r in m.Children.OfType<XamlPropertyAssignmentNode>())
                                        {
                                            yield return r;
                                        }
                                    }
                                }

                                string key = keyNode.Text;

                                var styledElement = context.GetAvaloniaTypes().StyledElement;
                                var resource = context.ParentNodes()
                                                        .OfType<XamlAstConstructableObjectNode>()
                                                        .Where(o => styledElement.IsAssignableFrom(o.Type.GetClrType()))
                                                        .Select(o => o.Children.FirstOrDefault(p => matchProperty(p, styledElement, "Resources")))
                                                        .Where(r => r != null)
                                                        .SelectMany(r => getResourceValues(r))
                                                        .FirstOrDefault(r => getResourceValue_xKey(r) == key);

                                if (resource != null)
                                {
                                    startType = getResourceValue_Type(resource, startType);
                                }
                            }
                            break;

                        case XamlStaticExtensionNode staticExtension:
                            startType = staticExtension.Type?.GetClrType();
                            break;
                    }
                }

                if (dataTypeProperty?.Values.Count is 1 && dataTypeProperty.Values[0] is XamlAstTextNode text)
                {
                    startType = TypeReferenceResolver.ResolveType(context, text.Text, isMarkupExtension: false, text, strict: true).Type;
                }
                
                if (dataTypeProperty?.Values.Count is 1 && dataTypeProperty.Values[0] is XamlTypeExtensionNode typeNode)
                {
                    startType = typeNode.Value.GetClrType();
                }

                Func<IXamlType> startTypeResolver = startType is not null ? () => startType : () =>
                {
                    var parentDataContextNode = context.ParentNodes().OfType<AvaloniaXamlIlDataContextTypeMetadataNode>().FirstOrDefault();
                    if (parentDataContextNode is null)
                    {
                        throw new XamlBindingsTransformException("Cannot parse a compiled binding without an explicit x:DataType directive to give a starting data type for bindings.", binding);
                    }

                    return parentDataContextNode.DataContextType;
                };

                var selfType = context.ParentNodes().OfType<XamlAstConstructableObjectNode>().First().Type.GetClrType();
                
                // When using self bindings with setters we need to change target type to resolved selector type.
                if (context.GetAvaloniaTypes().SetterBase.IsAssignableFrom(selfType))
                {
                    selfType = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>().First().TargetType.GetClrType();
                }

                XamlIlBindingPathHelper.UpdateCompiledBindingExtension(context, binding, startTypeResolver, selfType);
            }

            return node;
        }
    }
}
