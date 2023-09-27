using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.UnitTests;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests;

internal class ViewModelForDisposingTest
{
    ~ViewModelForDisposingTest() { ; }
}

[DotMemoryUnit(FailIfRunWithoutSupport = false)]
public class DataContextTests
{
    public DataContextTests(ITestOutputHelper atr)
    {
        DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
    }
    
    [Fact]
    public void Window_DataContext_Disposed_After_Window_Close_With_Lifetime()
    {
        static IDisposable Run()
        {
            var unitTestApp = UnitTestApplication.Start(TestServices.StyledWindow);
            var lifetime = new ClassicDesktopStyleApplicationLifetime();
            lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var window = new Window { DataContext = new ViewModelForDisposingTest() };
            window.Show();
            window.Close();

            return Disposable.Create(lifetime, lt => lt.Shutdown())
                .DisposeWith(new CompositeDisposable(lifetime, unitTestApp));
        }

        using var _ = Run();
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        GC.Collect();

        dotMemory.Check(m => Assert.Equal(0, 
            m.GetObjects(o => o.Type.Is<ViewModelForDisposingTest>()).ObjectsCount));
    }
    
    [Fact]
    public void Window_DataContext_Disposed_After_Window_Close_Without_Lifetime()
    {
        static void Run()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            var window = new Window { DataContext = new ViewModelForDisposingTest() };
            window.Show();
            window.Close();
        }

        Run();
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        GC.Collect();
        
        dotMemory.Check(m => Assert.Equal(0, 
            m.GetObjects(o => o.Type.Is<ViewModelForDisposingTest>()).ObjectsCount));
    }
}
