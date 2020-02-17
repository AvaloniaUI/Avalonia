using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class StyleAttachBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private readonly TextBox _control;

        public StyleAttachBenchmark()
        {
            _app = UnitTestApplication.Start(
                TestServices.StyledWindow.With(
                    renderInterface: new NullRenderingPlatform(),
                    threadingInterface: new NullThreadingPlatform()));

            _root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer(),
            };

            _control = new TextBox();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AttachTextBoxStyles()
        {
            var styles = UnitTestApplication.Current.Styles;

            styles.Attach(_control, UnitTestApplication.Current);

            styles.Detach();
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
