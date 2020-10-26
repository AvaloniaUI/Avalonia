using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using XamlNameReferenceGenerator.Infrastructure;
using XamlX;
using XamlX.Parsers;

namespace XamlNameReferenceGenerator.Parsers
{
    internal class XamlXNameReferenceXamlParser : INameReferenceXamlParser
    {
        private readonly CSharpCompilation _compilation;

        public XamlXNameReferenceXamlParser(CSharpCompilation compilation) => _compilation = compilation;

        public IReadOnlyList<(string TypeName, string Name)> GetNamedControls(string xaml)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            MiniCompiler
                .CreateDefault(new RoslynTypeSystem(_compilation))
                .Transform(parsed);
            
            var visitor = new MiniNamedControlCollector();
            parsed.Root.Visit(visitor);
            parsed.Root.VisitChildren(visitor);
            return visitor.Controls;
        }
    }
}