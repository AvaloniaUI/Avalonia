using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common.Domain;

namespace Avalonia.Generators.Common;

internal class GlobPatternGroup : IGlobPattern
{
    private readonly EquatableList<GlobPattern> _patterns;

    public GlobPatternGroup(IEnumerable<string> patterns) =>
        _patterns = new EquatableList<GlobPattern>(patterns.Select(p => new GlobPattern(p)));

    public bool Matches(string str) => _patterns.Any(pattern => pattern.Matches(str));

    public bool Equals(IGlobPattern other) => _patterns.Any(pattern => pattern.Equals(other));
    public override int GetHashCode() => _patterns.GetHashCode();
    public override bool Equals(object? obj) => obj is GlobPattern pattern && Equals(pattern);
    public override string ToString() => $"[{string.Join(", ", _patterns.Select(p => p.ToString()))}]";
}
