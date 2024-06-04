using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using XamlParseException = XamlX.XamlParseException;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathParser : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                var convertedNode = ConvertLongFormPropertiesToBindingExpressionNode(context, binding);
                var foundPath = false;

                if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlAstTextNode bindingPathText)
                {
                    var reader = new CharacterReader(bindingPathText.Text.AsSpan());
                    var (nodes, _) = BindingExpressionGrammar.Parse(ref reader);

                    if (convertedNode != null)
                    {
                        nodes.Insert(nodes.TakeWhile(x => x is BindingExpressionGrammar.ITransformNode).Count(), convertedNode);
                    }

                    if (nodes.Count == 1 && nodes[0] is BindingExpressionGrammar.EmptyExpressionNode)
                    {
                        binding.Arguments.RemoveAt(0);
                    }
                    else
                    {
                        binding.Arguments[0] = new ParsedBindingPathNode(bindingPathText, context.GetAvaloniaTypes().CompiledBindingPath, nodes);
                        foundPath = true;
                    }
                }
                
                if (!foundPath)
                {
                    var bindingPathAssignment = binding.Children.OfType<XamlAstXamlPropertyValueNode>()
                        .FirstOrDefault(v => v.Property.GetClrProperty().Name == "Path");

                    if (bindingPathAssignment != null && bindingPathAssignment.Values[0] is XamlAstTextNode pathValue)
                    {
                        var reader = new CharacterReader(pathValue.Text.AsSpan());
                        var (nodes, _) = BindingExpressionGrammar.Parse(ref reader);

                        if (nodes.Count == 1 && nodes[0] is BindingExpressionGrammar.EmptyExpressionNode)
                        {
                            bindingPathAssignment.Values.RemoveAt(0);
                        }
                        else
                        {
                            if (convertedNode != null)
                            {
                                nodes.Insert(nodes.TakeWhile(x => x is BindingExpressionGrammar.ITransformNode).Count(), convertedNode);
                            }

                            bindingPathAssignment.Values[0] = new ParsedBindingPathNode(pathValue, context.GetAvaloniaTypes().CompiledBindingPath, nodes);
                        }

                        foundPath = true;
                    }
                }

                if (!foundPath && convertedNode != null)
                {
                    var nodes = new List<BindingExpressionGrammar.INode> { convertedNode };
                    binding.Arguments.Add(new ParsedBindingPathNode(binding, context.GetAvaloniaTypes().CompiledBindingPath, nodes));
                }
            }

            return node;
        }

        private static BindingExpressionGrammar.INode ConvertLongFormPropertiesToBindingExpressionNode(
            AstTransformationContext context,
            XamlAstObjectNode binding)
        {
            BindingExpressionGrammar.INode convertedNode = null;

            var syntheticCompiledBindingProperties = binding.Children.OfType<XamlAstXamlPropertyValueNode>()
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

            var sourceProperty = binding.Children.OfType<XamlAstXamlPropertyValueNode>()
                .FirstOrDefault(v =>
                    v.Property is XamlAstClrProperty prop
                    && prop.Name == "Source");

            if (elementNameProperty?.Values[0] is XamlAstTextNode elementName)
            {
                convertedNode = new BindingExpressionGrammar.NameNode { Name = elementName.Text };
            }
            else if (elementNameProperty != null)
            {
                throw new XamlBindingsTransformException($"Invalid ElementName '{elementNameProperty.Values[0]}'.", elementNameProperty.Values[0]);
            }

            if (sourceProperty != null && convertedNode != null)
            {
                throw new XamlBindingsTransformException("Only one of ElementName, Source, or RelativeSource specified as a binding source. Only one property is allowed.", binding);
            }

            if (GetRelativeSourceObjectFromAssignment(
                context,
                relativeSourceProperty,
                out var relativeSourceObject))
            {
                if (convertedNode != null)
                {
                    throw new XamlBindingsTransformException("Only one of ElementName, Source, or RelativeSource specified as a binding source. Only one property is allowed.", binding);
                }

                var modeProperty = relativeSourceObject.Children
                    .OfType<XamlAstXamlPropertyValueNode>()
                    .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Mode")?
                    .Values.FirstOrDefault() as XamlAstTextNode
                    ?? relativeSourceObject.Arguments.OfType<XamlAstTextNode>().FirstOrDefault();
                
                var mode = modeProperty?.Text;
                if (relativeSourceObject.Arguments.Count == 0 && mode == null)
                {
                    mode = "FindAncestor";
                }

                if (mode == "FindAncestor")
                {
                    var ancestorLevel = relativeSourceObject.Children
                        .OfType<XamlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "FindAncestor")
                        ?.Values[0] is XamlAstTextNode ancestorLevelText ? int.Parse(ancestorLevelText.Text) - 1 : 0;

                    var treeType = relativeSourceObject.Children
                        .OfType<XamlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Tree")
                        ?.Values[0] is XamlAstTextNode treeTypeValue ? treeTypeValue.Text : "Visual";

                    var ancestorType = relativeSourceObject.Children
                        .OfType<XamlAstXamlPropertyValueNode>()
                        .FirstOrDefault(x => x.Property.GetClrProperty().Name == "AncestorType")
                        ?.Values[0] switch
                        {
                            XamlAstTextNode textNode => TypeReferenceResolver.ResolveType(
                                context,
                                textNode.Text,
                                false,
                                textNode,
                                true).GetClrType(),
                            XamlTypeExtensionNode typeExtensionNode => typeExtensionNode.Value.GetClrType(),
                            null => null,
                            _ => throw new XamlBindingsTransformException($"Unsupported node for AncestorType property", relativeSourceObject)
                        };

                    if (ancestorType is null)
                    {
                        if (treeType == "Visual")
                        {
                            throw new XamlBindingsTransformException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.", relativeSourceObject);
                        }
                        else if (treeType == "Logical")
                        {
                            var styledElementType = context.GetAvaloniaTypes().StyledElement;
                            ancestorType = context
                                .ParentNodes()
                                .OfType<XamlAstObjectNode>()
                                .Where(x => styledElementType.IsAssignableFrom(x.Type.GetClrType()))
                                .ElementAtOrDefault(ancestorLevel)
                                ?.Type.GetClrType();

                            if (ancestorType is null)
                            {
                                throw new XamlBindingsTransformException("Unable to resolve implicit ancestor type based on XAML tree.", relativeSourceObject);
                            }
                        }
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
                        throw new XamlBindingsTransformException($"Unknown tree type '{treeType}'.", binding);
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
                    var contentTemplateNode = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                        .FirstOrDefault(x =>
                            x.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate);
                    if (contentTemplateNode is null)
                    {
                        throw new XamlBindingsTransformException("A binding with a TemplatedParent RelativeSource has to be in a ControlTemplate.", binding);
                    }

                    var parentType = contentTemplateNode.TargetType.GetClrType();
                    if (parentType is null)
                    {
                        throw new XamlBindingsTransformException("TargetType has to be set on ControlTemplate or it should be defined inside of a Style.", binding);
                    } 

                    convertedNode = new TemplatedParentBindingExpressionNode { Type = parentType };
                }
                else
                {
                    throw new XamlBindingsTransformException($"Unknown RelativeSource mode '{mode}'.", binding);
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
            AstTransformationContext context,
            XamlAstXamlPropertyValueNode relativeSourceProperty,
            out XamlAstObjectNode relativeSourceObject)
        {
            relativeSourceObject = null;
            if (relativeSourceProperty is null)
            {
                return false;
            }

            if (relativeSourceProperty.Values[0] is XamlMarkupExtensionNode me)
            {
                if (me.Type.GetClrType() != context.GetAvaloniaTypes().RelativeSource)
                {
                    throw new XamlBindingsTransformException($"Expected an object of type 'Avalonia.Data.RelativeSource'. Found a object of type '{me.Type.GetClrType().GetFqn()}'", me);
                }

                relativeSourceObject = (XamlAstObjectNode)me.Value;
                return true;
            }

            if (relativeSourceProperty.Values[0] is XamlAstObjectNode on)
            {
                if (on.Type.GetClrType() != context.GetAvaloniaTypes().RelativeSource)
                {
                    throw new XamlBindingsTransformException($"Expected an object of type 'Avalonia.Data.RelativeSource'. Found a object of type '{on.Type.GetClrType().GetFqn()}'", on);
                }

                relativeSourceObject = on;
                return true;
            }

            return false;
        }
    }

    class ParsedBindingPathNode : XamlAstNode, IXamlAstValueNode
    {
        public ParsedBindingPathNode(IXamlLineInfo lineInfo, IXamlType compiledBindingType, IList<BindingExpressionGrammar.INode> path)
            : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, compiledBindingType, false);
            Path = path;
        }

        public IXamlAstTypeReference Type { get; }

        public IList<BindingExpressionGrammar.INode> Path { get; }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            for (int i = 0; i < Path.Count; i++)
            {
                if (Path[i] is IXamlAstNode ast)
                {
                    Path[i] = (BindingExpressionGrammar.INode)ast.Visit(visitor);
                }
            }
        }
    }

    class VisualAncestorBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlType Type { get; set; }
        public int Level { get; set; }
    }

    class LogicalAncestorBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlType Type { get; set; }
        public int Level { get; set; }
    }

    class TemplatedParentBindingExpressionNode : BindingExpressionGrammar.INode
    {
        public IXamlType Type { get; set; }
    }

    class RawSourceBindingExpressionNode : XamlAstNode, BindingExpressionGrammar.INode
    {
        public RawSourceBindingExpressionNode(IXamlAstValueNode rawSource)
            : base(rawSource)
        {
            RawSource = rawSource;
        }

        public IXamlAstValueNode RawSource { get; private set; }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            RawSource = (IXamlAstValueNode)RawSource.Visit(visitor);
        }
    }
}
