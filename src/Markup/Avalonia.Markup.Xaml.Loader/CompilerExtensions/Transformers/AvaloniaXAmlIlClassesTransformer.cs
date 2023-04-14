using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    /// <summary>
    /// Converts an attribute syntax property value assignment to a collection syntax property
    /// assignment.
    /// </summary>
    /// <remarks>
    /// Converts the property assignment `Classes="foo bar"` to:
    /// 
    /// <code>
    ///     <StyledElement.Classes>
    ///         <x:String>foo</String>
    ///         <x:String>bar</String>
    ///     </StyledElement.Classes>
    /// </code>
    /// </remarks>
    class AvaloniaXAmlIlClassesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            var types = context.GetAvaloniaTypes();

            if (node is XamlAstXamlPropertyValueNode propertyValue &&
                propertyValue.IsAttributeSyntax &&
                propertyValue.Property is XamlAstClrProperty property &&
                property.Getter?.ReturnType.Equals(types.Classes) == true &&
                propertyValue.Values.Count == 1 &&
                propertyValue.Values[0] is XamlAstTextNode value)
            {
                var classes = value.Text.Split(' ');
                var stringType = context.Configuration.WellKnownTypes.String;
                return new XamlAstXamlPropertyValueNode(
                    node,
                    property,
                    classes.Select(x => new XamlAstTextNode(node, x, type: stringType)),
                    false);
            }

            return node;
        }
    }
}
