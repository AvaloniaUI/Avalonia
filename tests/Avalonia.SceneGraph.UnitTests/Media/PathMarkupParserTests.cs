// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Platform;
using Moq;
using Xunit;

namespace Avalonia.SceneGraph.UnitTests.Media
{
    public class PathMarkupParserTests
    {
        [Fact]
        public void Parses_Move()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var result = new Mock<IStreamGeometryContextImpl>();

                var parser = PrepareParser(result);

                parser.Parse("M10 10");

                result.Verify(x => x.BeginFigure(new Point(10, 10), true));
            }
        }

        [Fact]
        public void Parses_Line()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var result = new Mock<IStreamGeometryContextImpl>();

                var parser = PrepareParser(result);

                parser.Parse("M0 0L10 10");

                result.Verify(x => x.LineTo(new Point(10, 10)));
            }
        }

        [Fact]
        public void Parses_Close()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var result = new Mock<IStreamGeometryContextImpl>();

                var parser = PrepareParser(result);

                parser.Parse("M0 0L10 10z");

                result.Verify(x => x.EndFigure(true));
            }
        }

        [Theory]
        [InlineData("M0 0L10 10z")]
        [InlineData("M50 50 L100 100 L150 50")]
        [InlineData("M50 50L100 100L150 50")]
        [InlineData("M50,50 L100,100 L150,50")]
        [InlineData("M50 50 L-10 -10 L10 50")]
        [InlineData("M50 50L-10-10L10 50")]
        [InlineData("M50 50 L100 100 L150 50zM50 50 L70 70 L120 50z")]
        [InlineData("M 50 50 L 100 100 L 150 50")]
        [InlineData("M50 50 L100 100 L150 50 H200 V100Z")]
        [InlineData("M 80 200 A 100 50 45 1 0 100 50")]
        [InlineData(
            "F1 M 16.6309 18.6563C 17.1309 8.15625 29.8809 14.1563 29.8809 14.1563C 30.8809 11.1563 34.1308 11.4063" +
            " 34.1308 11.4063C 33.5 12 34.6309 13.1563 34.6309 13.1563C 32.1309 13.1562 31.1309 14.9062 31.1309 14.9" +
            "062C 41.1309 23.9062 32.6309 27.9063 32.6309 27.9062C 24.6309 24.9063 21.1309 22.1562 16.6309 18.6563 Z" +
            " M 16.6309 19.9063C 21.6309 24.1563 25.1309 26.1562 31.6309 28.6562C 31.6309 28.6562 26.3809 39.1562 18" +
            ".3809 36.1563C 18.3809 36.1563 18 38 16.3809 36.9063C 15 36 16.3809 34.9063 16.3809 34.9063C 16.3809 34" +
            ".9063 10.1309 30.9062 16.6309 19.9063 Z ")]
        public void Should_Parse(string pathData)
        {
            using (AvaloniaLocator.EnterScope())
            {
                var parser = PrepareParser();

                parser.Parse(pathData);

                Assert.True(true);
            }
        }

        private static PathMarkupParser PrepareParser(Mock<IStreamGeometryContextImpl> implMock = null)
        {
            AvaloniaLocator.CurrentMutable
                    .Bind<IPlatformRenderInterface>()
                    .ToConstant(Mock.Of<IPlatformRenderInterface>());

            return new PathMarkupParser(
                new StreamGeometry(),
                new StreamGeometryContext(implMock != null ? implMock.Object : Mock.Of<IStreamGeometryContextImpl>()));
        }
    }
}