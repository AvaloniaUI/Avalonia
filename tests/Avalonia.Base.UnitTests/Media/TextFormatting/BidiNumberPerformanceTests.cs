using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BidiNumberPerformanceTests
{
    [Fact]
    public void A_Long_Leading_Number_Sequence_Is_Resolved_Without_Repeated_Prefix_Scans()
    {
        var classes = Enumerable.Repeat(BidiClass.EuropeanNumber, 60_000).ToArray();
        var algorithm = new BidiAlgorithm();
        var timer = Stopwatch.StartNew();
        algorithm.Process(classes, ArraySlice<BidiPairedBracketType>.Empty, ArraySlice<int>.Empty,
            0, false, null, null, null);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"Bidi processing took {timer.Elapsed}.");
        Assert.Equal(classes.Length, algorithm.ResolvedLevels.Length);
        for (var i = 0; i < classes.Length; i++)
            Assert.Equal(0, algorithm.ResolvedLevels[i]);
    }
}
