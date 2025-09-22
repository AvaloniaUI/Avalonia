using System;

namespace Avalonia.Generators.Common.Domain;

internal interface IGlobPattern : IEquatable<IGlobPattern>
{
    bool Matches(string str);
}
