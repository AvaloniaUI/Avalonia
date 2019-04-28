using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class AvaloniaXamlIlAvaloniaPropertyResolver : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstClrPropertyReference prop)
            {
                var n = prop.Property.Name + "Property";
                var field =
                    (prop.Property.Getter ?? prop.Property.Setter).DeclaringType.Fields
                    .FirstOrDefault(f => f.Name == n);
                if (field != null)
                    prop.Property = new XamlIlAvaloniaProperty(prop.Property, field);
            }

            return node;
        }
    }
}
