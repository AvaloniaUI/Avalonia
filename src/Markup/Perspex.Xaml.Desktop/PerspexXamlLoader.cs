namespace Perspex.Xaml.Desktop
{
    using OmniXaml;

    public class PerspexXamlLoader : XamlLoader
    {
        public PerspexXamlLoader(ITypeFactory typeFactory) : base(new PerspexParserFactory(typeFactory))
        {            
        }
    }
}