using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    internal class XDataTypeTransformer : IXamlAstTransformer
    {
        private const string DataTypePropertyName = "DataType";
        
        /// <summary>
        /// Converts x:DataType directives to regular DataType assignments if property with Avalonia.Metadata.DataTypeAttribute exists.
        /// </summary>
        /// <returns></returns>
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on)
            {
                for (var c = 0; c < on.Children.Count; c++)
                {
                    var ch = on.Children[c];
                    if (ch is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: DataTypePropertyName } d)
                    {
                        if (on.Children.OfType<XamlAstXamlPropertyValueNode>()
                            .Any(p => ((XamlAstNamePropertyReference)p.Property)?.Name == DataTypePropertyName))
                        {
                            // Break iteration if any DataType property was already set by user code.
                            break;
                        }
                        
                        var templateDataTypeAttribute = context.GetAvaloniaTypes().DataTypeAttribute;

                        var clrType = (on.Type as XamlAstClrTypeReference)?.Type;
                        if (clrType is null)
                        {
                            break;
                        }

                        // Technically it's possible to map "x:DataType" to a property with [DataType] attribute regardless of its name,
                        // but we go explicitly strict here and check the name as well.
                        var (declaringType, dataTypeProperty) = GetAllProperties(clrType)
                            .FirstOrDefault(t => t.property.Name == DataTypePropertyName && t.property.CustomAttributes
                                .Any(a => a.Type == templateDataTypeAttribute));
                       
                        if (dataTypeProperty is not null)
                        {
                            on.Children[c] = new XamlAstXamlPropertyValueNode(d,
                                new XamlAstNamePropertyReference(d,
                                    new XamlAstClrTypeReference(ch, declaringType, false), dataTypeProperty.Name,
                                    on.Type),
                                d.Values,
                                true);
                        }
                    }
                }
            }

            return node;
        }
        
        private static IEnumerable<(IXamlType declaringType, IXamlProperty property)> GetAllProperties(IXamlType t)
        {
            foreach (var p in t.Properties)
                yield return (t, p);
            if(t.BaseType!=null)
                foreach (var tuple in GetAllProperties(t.BaseType))
                    yield return tuple;
        }
    }
}
