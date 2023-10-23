using System;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class Utf16UtilsTests
{
    [Theory,
        InlineData("\ud87e\udc32123", 1, 2),
        InlineData("\ud87e\udc32123", 2, 3),
        InlineData("test", 3, 3),
        InlineData("\ud87e\udc32", 0, 0),
        InlineData("12\ud87e\udc3212", 2, 2),
        InlineData("12\ud87e\udc3212", 3, 4),
    ]
    public void CharacterOffsetToStringOffset(string s, int charOffset, int stringOffset)
    {
        Assert.Equal(stringOffset, Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, false));
    }
    
    [Theory,
     InlineData("\ud87e\udc32", 2, true),
     InlineData("12", 2, true),
    ]
    public void CharacterOffsetToStringOffsetThrowsOnOutOfRange(string s, int charOffset, bool throws)
    {
        if (throws)
            Assert.Throws<IndexOutOfRangeException>(() =>
                Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, true));
        else
            Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, true);
    }
}