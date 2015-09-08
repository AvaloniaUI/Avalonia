





namespace Perspex.Markup.Xaml.Templates
{
    using System.Collections.Generic;
    using OmniXaml;

    public class TemplateLoader : IDeferredLoader
    {
        public object Load(IEnumerable<XamlInstruction> nodes, IWiringContext context)
        {
            return new TemplateContent(nodes, context);
        }
    }
}