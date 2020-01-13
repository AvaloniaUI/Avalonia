using System;

using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.UnitTests;
using Avalonia.Utility;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class TextFormatterTests
    {
        //[Fact]
        //public void Should_Split_TextRunProperties()
        //{
        //    using (UnitTestApplication.Start(
        //        TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
        //            textFormatterImpl: new TextFormatterImpl())))
        //    {
        //        var text = new ReadOnlySlice<char>("0123456789".AsMemory());

        //        var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

        //        var textRuns = new List<TextStyleRun>
        //        {
        //            new TextStyleRun(new TextPointer(0, 3), defaultTextRunStyle ),
        //            new TextStyleRun(new TextPointer(3, 3), defaultTextRunStyle ),
        //            new TextStyleRun(new TextPointer(6, 3), defaultTextRunStyle ),
        //            new TextStyleRun(new TextPointer(9, 1), defaultTextRunStyle )
        //        };

        //        for (var i = 1; i < text.Length; i++)
        //        {
        //            var result = TextFormatter.SplitTextRunProperties(textRuns, i);

        //            var firstSum = result.First.Sum(x => x.TextPointer.Length);

        //            var secondSum = result.Second?.Sum(x => x.TextPointer.Length) ?? 0;

        //            Assert.Equal(text.Length, firstSum + secondSum);
        //        }
        //    }
        //}

        [Fact]
        public void Should_Format_TextRuns_With_Default_Style()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatterImpl: new TextFormatterImpl())))
            {
                var text = new ReadOnlySlice<char>("0123456789".AsMemory());

                var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var textRuns = TextFormatter.FormatTextRuns(text, defaultTextRunStyle);

                Assert.Equal(1, textRuns.Count);

                Assert.Equal(defaultTextRunStyle.TextFormat, textRuns[0].TextFormat);

                Assert.Equal(defaultTextRunStyle.Foreground, textRuns[0].Foreground);

                Assert.Equal(text.Length, textRuns[0].GlyphRun.Characters.Length);
            }
        }

        [Fact]
        public void Should_Get_TextStyleRun()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatterImpl: new TextFormatterImpl())))
            {
                var text = new ReadOnlySlice<char>("0123456789".AsMemory());

                var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var overrideStyle = new TextStyle(Typeface.Default, 14, Brushes.White);

                ReadOnlySpan<TextStyleRun> textStyleRuns = new[]
                {
                    new TextStyleRun(new TextPointer(3, 4), overrideStyle)
                };

                var textStyleRun = TextFormatter.CreateTextStyleRunWithOverride(text, defaultTextRunStyle, ref textStyleRuns);

                Assert.Equal(1, textStyleRuns.Length);

                Assert.Equal(3, textStyleRun.TextPointer.Length);

                Assert.Equal(defaultTextRunStyle.TextFormat, textStyleRun.Style.TextFormat);

                text = text.Skip(3);

                textStyleRun = TextFormatter.CreateTextStyleRunWithOverride(text, defaultTextRunStyle, ref textStyleRuns);

                Assert.Equal(0, textStyleRuns.Length);

                Assert.Equal(4, textStyleRun.TextPointer.Length);

                Assert.Equal(overrideStyle.TextFormat, textStyleRun.Style.TextFormat);

                text = text.Skip(4);

                textStyleRun = TextFormatter.CreateTextStyleRunWithOverride(text, defaultTextRunStyle, ref textStyleRuns);

                Assert.Equal(3, textStyleRun.TextPointer.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_TextRunStyles()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface(null),
                    textFormatterImpl: new TextFormatterImpl())))
            {
                var text = new ReadOnlySlice<char>("0123456789".AsMemory());

                var defaultTextRunStyle = new TextStyle(Typeface.Default, 12, Brushes.Black);

                var textStyleRuns = new[]
                {
                    new TextStyleRun(new TextPointer(0, 3), defaultTextRunStyle ),
                    new TextStyleRun(new TextPointer(3, 3), new TextStyle(Typeface.Default, 13, Brushes.Black) ),
                    new TextStyleRun(new TextPointer(6, 3), new TextStyle(Typeface.Default, 14, Brushes.Black) ),
                    new TextStyleRun(new TextPointer(9, 1), defaultTextRunStyle )
                };

                var textRuns = TextFormatter.FormatTextRuns(text, defaultTextRunStyle, textStyleRuns);

                Assert.Equal(textStyleRuns.Length, textRuns.Count);

                for (var i = 0; i < textStyleRuns.Length; i++)
                {
                    var textStyleRun = textStyleRuns[i];

                    var textRun = textRuns[i];

                    Assert.Equal(textStyleRun.TextPointer.Length, textRun.GlyphRun.Characters.Length);
                }
            }
        }
    }
}
