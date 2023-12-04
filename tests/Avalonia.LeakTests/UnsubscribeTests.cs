using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.UnitTests;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests;

public class UnsubscribeTests
{
    public UnsubscribeTests(ITestOutputHelper atr)
    {
        DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
    }

    [Fact]
    public void Unsubscribe_After_Window_Close_Without_Lifetime()
    {
        static void Run()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            var window = new Window();
            window.Bind(TemplatedControl.FontFamilyProperty, new FontObservable().ToBinding());
            window.Show();
            window.Close();
        }

        Run();
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

        Assert.Empty(FontSel.s_fontList);
    }

    [Fact]
    public void Content_Unsubscribe_After_Window_Close_Without_Lifetime()
    {
        static void Run()
        {
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            
            var control = new UserControl();
            control.Bind(TemplatedControl.FontFamilyProperty, new FontObservable().ToBinding());
            var window = new Window
            {
                Content = control
            };
            window.Show();
            window.Close();
        }

        Run();
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

        Assert.Empty(FontSel.s_fontList);
    }

    [Fact]
    public void Content_Unsubscribe()
    {
        static void Run()
        {
            var control = new UserControl();
            control.Bind(TemplatedControl.FontFamilyProperty, new FontObservable().ToBinding());
            control.CloseAllObserverCore();
        }

        Run();
        // Process all Loaded events to free control reference(s)
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

        Assert.Empty(FontSel.s_fontList);
    }

    private static class FontSel
    {
        public static readonly List<IObserver<FontFamily>> s_fontList = new();

        public static IDisposable Add(IObserver<FontFamily> observer)
        {
            s_fontList.Add(observer);
            observer.OnNext(FontFamily.Default);
            return new Unsubscribe(observer);
        }

        public static void Remove(IObserver<FontFamily> observer)
        {
            s_fontList.Remove(observer);
        }

        private class Unsubscribe : IDisposable
        {
            private IObserver<FontFamily> _observer;
            public Unsubscribe(IObserver<FontFamily> observer)
            {
                _observer = observer;

            }

            public void Dispose()
            {
                Remove(_observer);
            }
        }
    }

    private class FontObservable : IObservable<FontFamily>
    {
        public IDisposable Subscribe(IObserver<FontFamily> observer)
        {
            return FontSel.Add(observer);
        }
    }
}
