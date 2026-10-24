using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BidiNumberPerformanceTests
{
    public static IEnumerable<object[]> StrongContexts()
    {
        yield return [new[] { BidiClass.LeftToRight, BidiClass.EuropeanNumber, BidiClass.EuropeanNumber },
            new sbyte[] { 0, 0, 0 }];
        yield return [new[] { BidiClass.RightToLeft, BidiClass.EuropeanNumber, BidiClass.EuropeanNumber },
            new sbyte[] { 1, 2, 2 }];
        yield return [new[] { BidiClass.ArabicLetter, BidiClass.NonspacingMark, BidiClass.EuropeanNumber },
            new sbyte[] { 1, 1, 2 }];
        yield return [new[] { BidiClass.ArabicLetter, BidiClass.EuropeanNumber, BidiClass.LeftToRight,
            BidiClass.EuropeanNumber, BidiClass.ArabicLetter, BidiClass.EuropeanNumber },
            new sbyte[] { 1, 2, 0, 0, 1, 2 }];
    }

    [Theory]
    [MemberData(nameof(StrongContexts))]
    public void Number_Resolution_Retains_The_Preceding_Strong_Context(BidiClass[] classes, sbyte[] expected)
    {
        var algorithm = new BidiAlgorithm();
        Process(algorithm, classes);
        Assert.Equal(expected, algorithm.ResolvedLevels.ToArray());
    }

    [Fact]
    public void Strong_Context_Does_Not_Leak_Between_Isolates_Or_Paragraphs()
    {
        var algorithm = new BidiAlgorithm();
        Process(algorithm, [BidiClass.ArabicLetter, BidiClass.LeftToRightIsolate,
            BidiClass.EuropeanNumber, BidiClass.PopDirectionalIsolate, BidiClass.EuropeanNumber]);
        Assert.Equal(2, algorithm.ResolvedLevels[2]);
        Assert.Equal(2, algorithm.ResolvedLevels[4]);
        Process(algorithm, [BidiClass.EuropeanNumber, BidiClass.EuropeanNumber]);
        Assert.Equal(new sbyte[] { 0, 0 }, algorithm.ResolvedLevels.ToArray());
    }

    private static void Process(BidiAlgorithm algorithm, BidiClass[] classes)
        => algorithm.Process(classes, ArraySlice<BidiPairedBracketType>.Empty, ArraySlice<int>.Empty,
            0, false, null, null, null);

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
