using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class On<TReturn>
{
    public IReadOnlyList<string> Options { get; } = new List<string>();

    [Content]
    public TReturn? Content { get; set; }
}

public class On : On<object> {}
