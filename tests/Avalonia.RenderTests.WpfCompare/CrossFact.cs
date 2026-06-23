using System.Runtime.CompilerServices;
using Xunit;

namespace Avalonia.RenderTests.WpfCompare;

public class CrossFactAttribute : StaFactAttribute
{
    public CrossFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}

public class CrossTheoryAttribute : StaTheoryAttribute
{
    public CrossTheoryAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}
