using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlBindingPathParser : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode binding && binding.Type.GetClrType().Equals(context.GetAvaloniaTypes().CompiledBindingExtension))
            {
                if (binding.Arguments.Count > 0 && binding.Arguments[0] is XamlIlAstTextNode bindingPathText)
                {
                    var reader = new CharacterReader(bindingPathText.Text.AsSpan());
                    var (nodes, _) = BindingExpressionGrammar.Parse(ref reader);
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
                        bindingPathAssignment.Values[0] = new ParsedBindingPathNode(pathValue, context.GetAvaloniaTypes().CompiledBindingPath, nodes);
                    }
                }
            }

            return node;
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
}
