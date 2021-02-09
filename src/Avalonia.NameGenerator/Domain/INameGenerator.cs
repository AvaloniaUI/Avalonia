using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator.Domain
{
    internal interface INameGenerator
    {
        IReadOnlyList<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles);
    }

    internal record GeneratedPartialClass
    {
        public string FileName { get; }
        public string Content { get; }

        public GeneratedPartialClass(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}