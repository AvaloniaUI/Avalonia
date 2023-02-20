using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.Domain;

internal interface INameGenerator
{
    IReadOnlyList<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles);
}

internal record GeneratedPartialClass(string FileName, string Content);