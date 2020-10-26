using System.Collections.Generic;

namespace XamlNameReferenceGenerator.Parsers
{
    public interface INameReferenceXamlParser
    {
        List<(string TypeName, string Name)> GetNamedControls(string xaml);
    }
}