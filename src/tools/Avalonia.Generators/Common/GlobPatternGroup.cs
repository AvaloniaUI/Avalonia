using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common.Domain;

namespace Avalonia.Generators.Common;

internal class GlobPatternGroup(IEnumerable<string> patterns)
    : EquatableList<GlobPattern>(patterns.Select(p => new GlobPattern(p)).ToArray()), IGlobPattern
{
    public bool Matches(string str)
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].Matches(str))
                return true;
        }
        return false;
    }

    public bool Equals(IGlobPattern other) => other is GlobPatternGroup group && base.Equals(group);
    public override string ToString() => $"[{string.Join(", ", this.Select(p => p.ToString()))}]";
}

