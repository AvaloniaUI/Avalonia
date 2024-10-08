using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.NameGenerator;

internal interface INameGenerator
{
    IEnumerable<GeneratedPartialClass> GenerateSupportClasses(IEnumerable<AdditionalText> additionalFiles, CancellationToken cancellationToken);
}

internal record GeneratedPartialClass(string FileName, string Content);
