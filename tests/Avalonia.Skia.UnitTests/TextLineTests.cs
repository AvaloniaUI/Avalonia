using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.UnitTests;
using Avalonia.Utility;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class TextLineTests
    {
        [Fact]
        public void Should_Get_Previous_CharacterHit()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatterImpl: new TextFormatterImpl())))
            {
                var text = new ReadOnlySlice<char>("0123456789".AsMemory());

                var runPointers = new[] { new TextPointer(0, 3), new TextPointer(3, 3), new TextPointer(6, 4) };

                var runProperties = runPointers.Select(CreateRunProperties).ToList();

                var textLine = TextFormatter.FormatLine(text, double.PositiveInfinity, new TextParagraphProperties(),
                    runProperties);
            }
        }

        private static TextRunProperties CreateRunProperties(TextPointer textPointer)
        {
            return new TextRunProperties(textPointer, new TextRunStyle(Typeface.Default, 12, null));
        }
    }
}
