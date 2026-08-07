using Avalonia.Input.TextInput;
using Avalonia.Win32.Input;
using Xunit;

namespace Avalonia.IntegrationTests.Win32;

public class Imm32InputMethodTests
{
    [Fact]
    public void NormalizeClauseOffsets_Should_Support_Character_Offsets()
    {
        var actual = Imm32InputMethod.NormalizeClauseOffsets([0, 2, 5], 5);

        Assert.Equal([0, 2, 5], actual);
    }

    [Fact]
    public void NormalizeClauseOffsets_Should_Support_Byte_Offsets()
    {
        var actual = Imm32InputMethod.NormalizeClauseOffsets([0, 4, 10], 5);

        Assert.Equal([0, 2, 5], actual);
    }

    [Fact]
    public void BuildCompositionSegments_Should_Use_Target_Clause_When_Present()
    {
        var actual = Imm32InputMethod.BuildCompositionSegments(
            [0, 2, 4, 6],
            [0x02, 0x02, 0x01, 0x01, 0x02, 0x02],
            6,
            1);

        Assert.Collection(actual!,
            segment => Assert.Equal(new TextInputMethodPreeditSegment(0, 2, TextInputMethodPreeditSegmentKind.InactiveClause), segment),
            segment => Assert.Equal(new TextInputMethodPreeditSegment(2, 2, TextInputMethodPreeditSegmentKind.ActiveClause), segment),
            segment => Assert.Equal(new TextInputMethodPreeditSegment(4, 2, TextInputMethodPreeditSegmentKind.InactiveClause), segment));
    }

    [Fact]
    public void BuildCompositionSegments_Should_Fall_Back_To_Cursor_Clause_When_Target_Is_Missing()
    {
        var actual = Imm32InputMethod.BuildCompositionSegments(
            [0, 2, 4, 6],
            [0x02, 0x02, 0x02, 0x02, 0x02, 0x02],
            6,
            3);

        Assert.Collection(actual!,
            segment => Assert.Equal(TextInputMethodPreeditSegmentKind.InactiveClause, segment.Kind),
            segment => Assert.Equal(TextInputMethodPreeditSegmentKind.ActiveClause, segment.Kind),
            segment => Assert.Equal(TextInputMethodPreeditSegmentKind.InactiveClause, segment.Kind));
    }
}
