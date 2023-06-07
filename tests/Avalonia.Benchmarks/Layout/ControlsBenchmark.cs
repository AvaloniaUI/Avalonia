using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    [MemoryDiagnoser]
    public class ControlsBenchmark : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;

        public ControlsBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            _root = new TestRoot(true, null)
            {
                Renderer = new NullRenderer()
            };

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateCalendar()
        {
            var calendar = new Calendar();

            _root.Child = calendar;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateCalendarWithLoaded()
        {
            using var subscription = Control.LoadedEvent.AddClassHandler<Control>((c, s) => { });

            var calendar = new Calendar();

            _root.Child = calendar;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateControl()
        {
            var control = new Control();
            
            _root.Child = control;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateDecorator()
        {
            var control = new Decorator();
            
            _root.Child = control;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateScrollViewer()
        {
            var control = new ScrollViewer();
            
            _root.Child = control;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateButton()
        {
            var button = new Button();

            _root.Child = button;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreateTextBox()
        {
            var textBox = new TextBox();

            _root.Child = textBox;

            _root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
