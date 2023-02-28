using System.Text.RegularExpressions;
using Avalonia.Generators.Common.Domain;

namespace Avalonia.Generators.Common;

internal class GlobPattern : IGlobPattern
{
    private const RegexOptions GlobOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
    private readonly Regex _regex;

    public GlobPattern(string pattern)
    {
        var expression = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        _regex = new Regex(expression, GlobOptions);
    }

    public bool Matches(string str) => _regex.IsMatch(str);
}
