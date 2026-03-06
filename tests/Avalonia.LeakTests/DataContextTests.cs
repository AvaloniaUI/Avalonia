using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.LeakTests;

internal class ViewModelForDisposingTest
{
    [SuppressMessage("ReSharper", "EmptyDestructor", Justification = "Needed for test")]
    ~ViewModelForDisposingTest() { }
}

public class DataContextTests : ScopedTestBase
{
    [Fact]
    public void Window_DataContext_Disposed_After_Window_Close_With_Lifetime()
    {
        static IDisposable Run(out WeakReference weakDataContext)
        {
            var unitTestApp = UnitTestApplication.Start(TestServices.StyledWindow);
            var lifetime = new ClassicDesktopStyleApplicationLifetime();
            lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var viewModel = new ViewModelForDisposingTest();
            var window = new Window { DataContext = viewModel };
            window.Show();
            window.Close();

            var disposable = Disposable.Create(lifetime, lt => lt.Shutdown())
                .DisposeWith(new CompositeDisposable(lifetime, unitTestApp));

            weakDataContext = new WeakReference(viewModel);
            return disposable;
        }

        using var _ = Run(out var weakDataContext);
        Assert.True(weakDataContext.IsAlive);

        CollectGarbage();

        Assert.False(weakDataContext.IsAlive);
    }
    
    [Fact]
    public void Window_DataContext_Disposed_After_Window_Close_Without_Lifetime()
    {
        static void Run(out WeakReference weakDataContext)
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            var viewModel = new ViewModelForDisposingTest();
            var window = new Window { DataContext = viewModel };
            window.Show();
            window.Close();

            weakDataContext = new WeakReference(viewModel);
        }

        Run(out var weakDataContext);
        Assert.True(weakDataContext.IsAlive);

        CollectGarbage();
        
        Assert.False(weakDataContext.IsAlive);
    }

    private static void CollectGarbage()
    {
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        GC.Collect();
    }
}
