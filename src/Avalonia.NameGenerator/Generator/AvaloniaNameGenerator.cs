using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.Domain;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator.Generator
{
    internal class AvaloniaNameGenerator : INameGenerator
    {
        private readonly IClassResolver _classes;
        private readonly INameResolver _names;
        private readonly ICodeGenerator _code;

        public AvaloniaNameGenerator(IClassResolver classes, INameResolver names, ICodeGenerator code)
        {
            _classes = classes;
            _names = names;
            _code = code;
        }

        public IReadOnlyList<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles)
        {
            var resolveViewsQuery =
                from file in additionalFiles
                where file.Path.EndsWith(".xaml") ||
                      file.Path.EndsWith(".paml") ||
                      file.Path.EndsWith(".axaml")
                let xaml = file.GetText()!.ToString()
                let type = _classes.ResolveClass(xaml)
                where type != null
                let className = type.ClassName
                let nameSpace = type.NameSpace
                select new ResolvedView(className, nameSpace, xaml);

            var query =
                from view in resolveViewsQuery.ToList()
                let names = _names.ResolveNames(view.Xaml)
                let code = _code.GenerateCode(view.ClassName, view.NameSpace, names)
                let fileName = $"{view.ClassName}.g.cs"
                select new GeneratedPartialClass(fileName, code);

            return query.ToList();
        }

        private record ResolvedView
        {
            public string ClassName { get; }
            public string NameSpace { get; }
            public string Xaml { get; }

            public ResolvedView(string className, string nameSpace, string xaml)
            {
                ClassName = className;
                NameSpace = nameSpace;
                Xaml = xaml;
            }
        }
    }
}