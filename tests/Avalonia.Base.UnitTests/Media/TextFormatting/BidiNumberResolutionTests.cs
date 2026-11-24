using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BidiNumberResolutionTests
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

        // EN and AN resolve to the same level on their own, so these rows observe the W2 conversion
        // through W4 and W5, which join separators and terminators to European numbers only.
        yield return [new[] { BidiClass.ArabicLetter, BidiClass.EuropeanNumber, BidiClass.EuropeanSeparator,
            BidiClass.EuropeanNumber },
            new sbyte[] { 1, 2, 1, 2 }];
        yield return [new[] { BidiClass.ArabicLetter, BidiClass.EuropeanTerminator, BidiClass.EuropeanNumber },
            new sbyte[] { 1, 1, 2 }];
        yield return [new[] { BidiClass.LeftToRight, BidiClass.EuropeanNumber, BidiClass.ArabicLetter,
            BidiClass.EuropeanNumber, BidiClass.EuropeanSeparator, BidiClass.EuropeanNumber },
            new sbyte[] { 0, 0, 1, 2, 1, 2 }];
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
}
