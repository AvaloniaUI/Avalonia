using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlAvaloniaPropertyResolver : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstClrProperty prop)
            {
                var n = prop.Name + "Property";
                var field =
                    prop.DeclaringType.Fields
                    .FirstOrDefault(f => f.Name == n);
                if (field != null)
                    return new XamlIlAvaloniaProperty(prop, field, context.GetAvaloniaTypes());
            }

            return node;
        }
    }
}
