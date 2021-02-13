using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.Domain;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator.Generator
{
    internal class AvaloniaNameGenerator : INameGenerator
    {
        private readonly IGlobPattern _pathPattern;
        private readonly IGlobPattern _namespacePattern;
        private readonly IViewResolver _classes;
        private readonly INameResolver _names;
        private readonly ICodeGenerator _code;

        public AvaloniaNameGenerator(
            IGlobPattern pathPattern,
            IGlobPattern namespacePattern,
            IViewResolver classes,
            INameResolver names,
            ICodeGenerator code)
        {
            _pathPattern = pathPattern;
            _namespacePattern = namespacePattern;
            _classes = classes;
            _names = names;
            _code = code;
        }

        public IReadOnlyList<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles)
        {
            var resolveViews =
                from file in additionalFiles
                where (file.Path.EndsWith(".xaml") ||
                       file.Path.EndsWith(".paml") ||
                       file.Path.EndsWith(".axaml")) &&
                      _pathPattern.Matches(file.Path)
                let xaml = file.GetText()!.ToString()
                let view = _classes.ResolveView(xaml)
                where view != null && _namespacePattern.Matches(view.Namespace)
                select view;

            var query =
                from view in resolveViews
                let names = _names.ResolveNames(view.Xaml)
                let code = _code.GenerateCode(view.ClassName, view.Namespace, names)
                let fileName = $"{view.ClassName}.g.cs"
                select new GeneratedPartialClass(fileName, code);

            return query.ToList();
        }
    }
}