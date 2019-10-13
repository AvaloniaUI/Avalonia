using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathParser : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                var convertedNode = ConvertLongFormPropertiesToBindingExpressionNode(context, binding);

                if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlIlAstTextNode bindingPathText)
                {
                    var reader = new CharacterReader(bindingPathText.Text.AsSpan());
                    var (nodes, _) = BindingExpressionGrammar.Parse(ref reader);

                    if (convertedNode != null)
                    {
                        nodes.Insert(nodes.TakeWhile(x => x is BindingExpressionGrammar.ITransformNode).Count(), convertedNode);
                    }

                    binding.Arguments[0] = new ParsedBindingPathNode(bindingPathText, context.GetAvaloniaTypes().CompiledBindingPath, nodes);
                }
                else
                {
                    var bindingPathAssignment = binding.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                        .FirstOrDefault(v => v.Property.GetClrProperty().Name == "Path");

                    if (bindingPathAssignment != null && bindingPathAssignment.Values[0] is XamlIlAstTextNode pathValue)
                    {
                        var reader = new CharacterReader(pathValue.Text.AsSpan());
                        var (nodes, _) = BindingExpressionGrammar.Parse(ref reader);

                        if (convertedNode != null)
                        {
                            nodes.Insert(nodes.TakeWhile(x => x is BindingExpressionGrammar.ITransformNode).Count(), convertedNode);
                        }

                        bindingPathAssignment.Values[0] = new ParsedBindingPathNode(pathValue, context.GetAvaloniaTypes().CompiledBindingPath, nodes);
                    }
                }
            }

            return node;
        }

        private static BindingExpressionGrammar.INode ConvertLongFormPropertiesToBindingExpressionNode(
            XamlIlAstTransformationContext context,
            XamlIlAstObjectNode binding)
        {
            BindingExpressionGrammar.INode convertedNode = null;

            var syntheticCompiledBindingProperties = binding.Children.OfType<XamlIlAstXamlPropertyValueNode>()
                .Where(v => v.Property is AvaloniaSyntheticCompiledBindingProperty)
                .ToList();

            var elementNameProperty = syntheticCompiledBindingProperties
                .FirstOrDefault(v =>
                    v.Property is AvaloniaSyntheticCompiledBindingProperty prop
                    && prop.Name == SyntheticCompiledBindingPropertyName.ElementName);

            var relativeSourceProperty = syntheticCompiledBindingProperties
                .FirstOrDefault(v =>
                    v.Property is AvaloniaSyntheticCompiledBindingProperty prop
                    && prop.Name == SyntheticCompiledBindingPropertyName.RelativeSource);

            if (elementNameProperty?.Values[0] is XamlIlAstTextNode elementName)
            {
                convertedNode = new BindingExpressionGrammar.NameNode { Name = elementName.Text };
            }
            else if (elementNameProperty != null)
            {
                throw new XamlIlParseException($"Invalid ElementName '{elementNameProperty.Values[0]}'.", elementNameProperty.Values[0]);
            }

            if (GetRelativeSourceObjectFromAssignment(
                context,
                relativeSourceProperty,
                out var relativeSourceObject))
            {
                if (convertedNode != null)
                {
                    throw new XamlIlParseException("Both ElementName and RelativeSource specified as a binding source. Only one property is allowed.", binding);
                }

                var mode = relativeSourceObject.Children
                    .OfType<XamlIlAstXamlPropertyValueNode>()
                    .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Mode")
                    ?.Values[0] is XamlIlAstTextNode modeAssignedValue ? modeAssignedValue.Text : null;
                if (relativeSourceObject.Arguments.Count == 0 && mode == null)
                {
                    mode = "FindAncestor";
                }

                if (mode == "FindAncestor")
                {
                    var ancestorLevel = relativeSourceObject.Children
                        .OfType<XamlIlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "FindAncestor")
                        ?.Values[0] is XamlIlAstTextNode ancestorLevelText ? int.Parse(ancestorLevelText.Text) - 1 : 0;

                    var treeType = relativeSourceObject.Children
                        .OfType<XamlIlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Tree")
                        ?.Values[0] is XamlIlAstTextNode treeTypeValue ? treeTypeValue.Text : "Visual";

                    var ancestorTypeName = relativeSourceObject.Children
                        .OfType<XamlIlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "AncestorType")
                        ?.Values[0] as XamlIlAstTextNode;

                    IXamlIlType ancestorType = null;
                    if (ancestorTypeName is null)
                    {
                        if (treeType == "Visual")
                        {
                            throw new XamlIlParseException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.", relativeSourceObject);
                        }
                        else if (treeType == "Logical")
                        {
                            var styledElementType = context.GetAvaloniaTypes().StyledElement;
                            ancestorType = context
                                .ParentNodes()
                                .OfType<XamlIlAstObjectNode>()
                                .Where(x => styledElementType.IsAssignableFrom(x.Type.GetClrType()))
                                .ElementAtOrDefault(ancestorLevel)
                                ?.Type.GetClrType();

                            if (ancestorType is null)
                            {
                                throw new XamlIlParseException("Unable to resolve implicit ancestor type based on XAML tree.", relativeSourceObject);
                            }
                        }
                    }
                    else
                    {
                        ancestorType = XamlIlTypeReferenceResolver.ResolveType(
                                            context,
                                            ancestorTypeName.Text,
                                            false,
                                            ancestorTypeName,
                                            true).GetClrType();
                    }

                    if (treeType == "Visual")
                    {
                        convertedNode = new VisualAncestorBindingExpressionNode
                        {
                            Type = ancestorType,
                            Level = ancestorLevel
                        };
                    }
                    else if (treeType == "Logical")
                    {
                        convertedNode = new LogicalAncestorBindingExpressionNode
                        {
                            Type = ancestorType,
                            Level = ancestorLevel
                        };
                    }
                    else
                    {
                        throw new XamlIlParseException($"Unknown tree type '{treeType}'.", binding);
                    }
                }
                else if (mode == "DataContext")
                {
                    convertedNode = null;
                }
                else if (mode == "Self")
                {
                    convertedNode = new BindingExpressionGrammar.SelfNode();
                }
                else if (mode == "TemplatedParent")
                {
                    var parentType = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                        .FirstOrDefault(x =>
                            x.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate)
                        ?.TargetType.GetClrType();

                    if (parentType is null)
                    {
                        throw new XamlIlParseException("A binding with a TemplatedParent RelativeSource has to be in a ControlTemplate.", binding);
                    }

                    convertedNode = new TemplatedParentBindingExpressionNode { Type = parentType };
                }
                else
                {
                    throw new XamlIlParseException($"Unknown RelativeSource mode '{mode}'.", binding);
                }
            }

            if (elementNameProperty != null)
            {
                binding.Children.Remove(elementNameProperty);
            }
            if (relativeSourceProperty != null)
            {
                binding.Children.Remove(relativeSourceProperty);
            }

            return convertedNode;
        }

        private static bool GetRelativeSourceObjectFromAssignment(
            XamlIlAstTransformationContext context,
            XamlIlAstXamlPropertyValueNode relativeSourceProperty,
            out XamlIlAstObjectNode relativeSourceObject)
        {
            relativeSourceObject = null;
            if (relativeSourceProperty is null)
            {
                return false;
            }

            if (relativeSourceProperty.Values[0] is XamlIlMarkupExtensionNode me)
            {
                if (me.Type.GetClrType() != context.GetAvaloniaTypes().RelativeSource)
                {
                    throw new XamlIlParseException($"Expected an object of type 'Avalonia.Data.RelativeSource'. Found a object of type '{me.Type.GetClrType().GetFqn()}'", me);
                }

                relativeSourceObject = (XamlIlAstObjectNode)me.Value;
                return true;
            }

            if (relativeSourceProperty.Values[0] is XamlIlAstObjectNode on)
            {
                if (on.Type.GetClrType() != context.GetAvaloniaTypes().RelativeSource)
                {
                    throw new XamlIlParseException($"Expected an object of type 'Avalonia.Data.RelativeSource'. Found a object of type '{on.Type.GetClrType().GetFqn()}'", on);
                }

                relativeSourceObject = on;
                return true;
            }

            return false;
        }
    }

    class ParsedBindingPathNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public ParsedBindingPathNode(IXamlIlLineInfo lineInfo, IXamlIlType compiledBindingType, IList<BindingExpressionGrammar.INode> path)
            : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, compiledBindingType, false);
            Path = path;
        }

        public IXamlIlAstTypeReference Type { get; }

        public IList<BindingExpressionGrammar.INode> Path { get; }
    }

    class VisualAncestorBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlIlType Type { get; set; }
        public int Level { get; set; }
    }

    class LogicalAncestorBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlIlType Type { get; set; }
        public int Level { get; set; }
    }

    class TemplatedParentBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlIlType Type { get; set; }
    }
}
