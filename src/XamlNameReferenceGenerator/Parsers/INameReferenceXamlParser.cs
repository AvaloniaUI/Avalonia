using System.Collections.Generic;

namespace XamlNameReferenceGenerator.Parsers
{
    internal interface INameReferenceXamlParser
    {
        IReadOnlyList<(string TypeName, string Name)> GetNamedControls(string xaml);
    }
}