using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Visuals.Media
{
    [MemoryDiagnoser]
    public class PathMarkupParserTests : IDisposable
    {
        private IDisposable _app;

        public PathMarkupParserTests()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        [Benchmark]
        public void Parse_Large_Path()
        {
            const string PathData = "F1 M 16.6309 18.6563C 17.1309 8.15625 29.8809 14.1563 29.8809 14.1563C 30.8809 11.1563 34.1308 11.4063" +
                                " 34.1308 11.4063C 33.5 12 34.6309 13.1563 34.6309 13.1563C 32.1309 13.1562 31.1309 14.9062 31.1309 14.9" +
                                "062C 41.1309 23.9062 32.6309 27.9063 32.6309 27.9062C 24.6309 24.9063 21.1309 22.1562 16.6309 18.6563 Z" +
                                " M 16.6309 19.9063C 21.6309 24.1563 25.1309 26.1562 31.6309 28.6562C 31.6309 28.6562 26.3809 39.1562 18" +
                                ".3809 36.1563C 18.3809 36.1563 18 38 16.3809 36.9063C 15 36 16.3809 34.9063 16.3809 34.9063C 16.3809 34" +
                                ".9063 10.1309 30.9062 16.6309 19.9063 Z ";

            var streamGeometry = new StreamGeometry();

            using (var context = streamGeometry.Open())
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(PathData);
            }
        }
    }
}
