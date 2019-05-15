using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlAvaloniaPropertyResolver : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstClrProperty prop)
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
