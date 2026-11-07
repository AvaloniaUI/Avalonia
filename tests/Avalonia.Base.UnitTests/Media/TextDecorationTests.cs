using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.Media;
using Xunit;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Media
{
    public class TextDecorationTests : ScopedTestBase
    {
        [Fact]
        public void Should_Parse_TextDecorations()
        {
            var baseline = TextDecorationCollection.Parse("baseline");

            Assert.Equal(TextDecorationLocation.Baseline, baseline[0].Location);

            var underline = TextDecorationCollection.Parse("underline");

            Assert.Equal(TextDecorationLocation.Underline, underline[0].Location);

            var overline = TextDecorationCollection.Parse("overline");

            Assert.Equal(TextDecorationLocation.Overline, overline[0].Location);

            var strikethrough = TextDecorationCollection.Parse("strikethrough");

            Assert.Equal(TextDecorationLocation.Strikethrough, strikethrough[0].Location);
        }

        [Fact]
        public void Predefined_TextDecorations_Should_Be_Usable_On_Another_Thread()
        {
            var underline = TextDecorations.Underline;

            Assert.Same(underline, TextDecorations.Underline);
            Assert.Equal(1, underline[0].StrokeThickness);

            TextDecorationCollection? otherThreadUnderline = null;
            Exception? otherThreadException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    otherThreadUnderline = TextDecorations.Underline;

                    Assert.Equal(1, otherThreadUnderline[0].StrokeThickness);
                    Assert.Equal(TextDecorationLocation.Underline, otherThreadUnderline[0].Location);
                    Assert.Equal(TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough[0].Location);
                    Assert.Equal(TextDecorationLocation.Overline, TextDecorations.Overline[0].Location);
                    Assert.Equal(TextDecorationLocation.Baseline, TextDecorations.Baseline[0].Location);
                }
                catch (Exception e)
                {
                    otherThreadException = e;
                }
            });

            thread.Start();
            thread.Join();

            if (otherThreadException is not null)
                ExceptionDispatchInfo.Throw(otherThreadException);

            Assert.NotSame(underline, otherThreadUnderline);
        }
    }
}
