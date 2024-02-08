using System.Collections.Generic;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class BoxShadowTests
    {
        [Theory]
        [MemberData(nameof(ParseGetData))]
        public void BoxShadow_Should_Parse(BoxShadow expected, string source)
        {
            var parsered = BoxShadow.Parse(source);
            Assert.Equal(expected.IsInset, parsered.IsInset);
            Assert.Equal(expected.OffsetX, parsered.OffsetX);
            Assert.Equal(expected.OffsetY, parsered.OffsetY);
            Assert.Equal(expected.Blur, parsered.Blur);
            Assert.Equal(expected.Spread, parsered.Spread);
            Assert.Equal(expected.Color, parsered.Color);
        }

        [Theory]
        [MemberData(nameof(ToStringGetData))]
        public void BoxShadows_Should_ToString(BoxShadows source, string expected) =>
            Assert.Equal(expected, source.ToString(), true);

        public static IEnumerable<object[]> ParseGetData()
        {
            foreach (var extraSpaces in new[] { false, true })
                foreach (var inset in new[] { false, true })
                    foreach (var color in new[] { "red", "#FF122403" })
                        for (var componentCount = 2; componentCount < 5; componentCount++)
                        {
                            var s = (inset ? "inset " : "") + "10 20";
                            if (componentCount > 2)
                            {
                                s += " 30";
                            }

                            if (componentCount > 3)
                            {
                                s += " 40";
                            }

                            s += " " + color;

                            if (extraSpaces)
                                s = " " + s.Replace(" ", "  ") + "   ";

                            var parsed = BoxShadow.Parse(s);
                            yield return new object[] { parsed, s };
                        }
        }

        public static IEnumerable<object[]> ToStringGetData()
        {
            yield return new object[]
            {
                new BoxShadows(
                    new BoxShadow()
                    {
                        OffsetX = -15,
                        OffsetY = 20,
                        Spread = 5,
                        Color = Colors.Red,
                    }),
                "-15 20 0 5 red"
            };
            yield return new object[]
            {
                new BoxShadows(
                    new BoxShadow()
                    {
                        IsInset = true,
                        OffsetX = -15,
                        OffsetY = 20,
                        Spread = 5,
                        Color = Colors.Red,
                    }),
                "inset -15 20 0 5 red"
            };
            yield return new object[]
            {
                new BoxShadows(
                    new BoxShadow()
                    {
                        OffsetX = -15,
                        OffsetY = 20,
                        Blur = 5,
                        Color = Colors.Red,
                    }),
                "-15 20 5 red"
            };
            yield return new object[]
            {
                new BoxShadows(
                    new BoxShadow()
                    {
                        OffsetX = -20,
                        OffsetY = -20,
                        Blur = 60,
                        Color = Color.Parse("#CCFFFFFF")
                    },
                    new BoxShadow[] { new()
                    {
                        OffsetX = 20,
                        OffsetY = 20,
                        Blur = 60,
                        Color = Color.Parse("#33000000")
                    } }),
                "-20 -20 60 #CCFFFFFF, 20 20 60 #33000000"
            };

        }
    }
}
