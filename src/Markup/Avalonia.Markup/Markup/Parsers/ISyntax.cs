// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Markup.Parsers
{
    public interface ISyntax
    {
    }

    public interface ITypeSyntax
    {
        string TypeName { get; set; }

        string Xmlns { get; set; }
    }
}
