using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common.Domain;

namespace Avalonia.Generators.Common;

internal class GlobPatternGroup : IGlobPattern
{
    private readonly GlobPattern[] _patterns;

    public GlobPatternGroup(IEnumerable<string> patterns) =>
        _patterns = patterns
            .Select(pattern => new GlobPattern(pattern))
            .ToArray();

    public bool Matches(string str) => _patterns.Any(pattern => pattern.Matches(str));
}
