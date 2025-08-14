using System.Text.RegularExpressions;
using Avalonia.Generators.Common.Domain;

namespace Avalonia.Generators.Common;

internal class GlobPattern : IGlobPattern
{
    private const RegexOptions GlobOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
    private readonly Regex _regex;
    private readonly string _pattern;

    public GlobPattern(string pattern)
    {
        _pattern = pattern;
        var expression = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        _regex = new Regex(expression, GlobOptions);
    }

    public bool Matches(string str) => _regex.IsMatch(str);

    public bool Equals(IGlobPattern other) => other is GlobPattern pattern && pattern._pattern == _pattern;
    public override int GetHashCode() => _pattern.GetHashCode();
    public override bool Equals(object? obj) => obj is GlobPattern pattern && Equals(pattern);
    public override string ToString() => _pattern;
}
