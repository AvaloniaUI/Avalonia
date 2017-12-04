using Avalonia.Markup.Xaml.Templates;
using avm = Avalonia.Metadata;
using pm = Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal static class AttributeExtensions
    {
        public static pm.XamlDeferLoadAttribute ToPortableXaml(this avm.TemplateContentAttribute attrib)
        {
            if (attrib == null)
            {
                return null;
            }

            return new pm.XamlDeferLoadAttribute(typeof(TemplateLoader), typeof(TemplateContent));
        }

        public static pm.AmbientAttribute ToPortableXaml(this avm.AmbientAttribute attrib)
        {
            if (attrib == null)
            {
                return null;
            }

            return new pm.AmbientAttribute();
        }

        public static pm.DependsOnAttribute ToPortableXaml(this avm.DependsOnAttribute attrib)
        {
            if (attrib == null)
            {
                return null;
            }

            return new pm.DependsOnAttribute(attrib.Name);
        }
    }
}