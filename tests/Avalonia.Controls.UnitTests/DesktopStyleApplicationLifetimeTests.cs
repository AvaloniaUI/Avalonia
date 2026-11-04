using System;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DesktopStyleApplicationLifetimeTests : ScopedTestBase
    {
        [Fact]
        public void Should_Set_ExitCode_After_Shutdown()
        {
            using (UnitTestApplication.Start(new TestServices()))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())    
            {
                Setup(lifetime);

                Dispatcher.UIThread.Post(() => lifetime.Shutdown(1337));
                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(1337, exitCode);
            }
        }
        
        
        [Fact]
        public void Should_Return_ExitCode_When_Shutdown_From_MainWindow_Opened()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var mainWindow = new Window();
                mainWindow.Opened += (_, _) => lifetime.Shutdown(1337);
                lifetime.MainWindow = mainWindow;

                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(1337, exitCode);
            }
        }

        [Fact]
        public void Should_Exit_When_MainWindow_Closed_From_Opened()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var mainWindow = new Window();
                mainWindow.Opened += (_, _) => mainWindow.Close();
                lifetime.MainWindow = mainWindow;

                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(0, exitCode);
                Assert.True(hasExit);
                Assert.Empty(lifetime.Windows);
            }
        }

        [Fact]
        public void Should_Close_All_Remaining_Open_Windows_After_Explicit_Exit_Call()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var windows = new List<Window> { new Window(), new Window(), new Window(), new Window() };

                foreach (var window in windows)
                {
                    window.Show();
                }
                Assert.Equal(4, lifetime.Windows.Count);
                lifetime.Shutdown();

                Assert.Empty(lifetime.Windows);
            }
        }
        
        [Fact]
        public void Should_Only_Exit_On_Explicit_Exit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.False(hasExit);

                lifetime.Shutdown();

                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Should_Exit_After_MainWindow_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var mainWindow = new Window();

                mainWindow.Show();

                lifetime.MainWindow = mainWindow;

                var window = new Window();

                window.Show();

                mainWindow.Close();

                Assert.True(hasExit);
            }
        }

        [Fact]
        public void OnMainWindowClose_Overrides_Secondary_Window_Cancellation()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Setup(lifetime);

                var hasExit = false;
                var secondaryWindowClosingExecuted = false;
                var secondaryWindowClosedExecuted = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var mainWindow = new Window();
                mainWindow.Show();

                lifetime.MainWindow = mainWindow;

                var window = new Window();
                window.Closing += (_, args) =>
                {
                    secondaryWindowClosingExecuted = true;
                    args.Cancel = true;
                };
                window.Closed += (_, _) =>
                {
                    secondaryWindowClosedExecuted = true;
                };
                window.Show();

                mainWindow.Close();

                Assert.True(secondaryWindowClosingExecuted);
                Assert.True(secondaryWindowClosedExecuted);
                Assert.True(hasExit);
            }
        }

        [Fact]
        public void OnMainWindowClose_Overrides_Secondary_Window_Cancellation_From_TryShutdown()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Setup(lifetime);

                var hasExit = false;
                var secondaryWindowClosingExecuted = false;
                var secondaryWindowClosedExecuted = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var mainWindow = new Window();
                mainWindow.Show();

                lifetime.MainWindow = mainWindow;

                var window = new Window();
                window.Closing += (_, args) =>
                {
                    secondaryWindowClosingExecuted = true;
                    args.Cancel = true;
                };
                window.Closed += (_, _) =>
                {
                    secondaryWindowClosedExecuted = true;
                };
                window.Show();

                lifetime.TryShutdown();

                Assert.True(secondaryWindowClosingExecuted);
                Assert.True(secondaryWindowClosedExecuted);
                Assert.True(hasExit);
            }
        }

        [Fact]
        public void Should_Exit_After_Last_Window_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Show_Should_Add_Window_To_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var window = new Window();

                window.Show();

                Assert.Equal(new[] { window }, lifetime.Windows);
            }
        }

        [Fact]
        public void Window_Should_Be_Added_To_OpenWindows_Only_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var window = new Window();

                window.Show();
                window.Show();
                window.IsVisible = true;

                Assert.Equal(new[] { window }, lifetime.Windows);

                window.Close();
            }
        }

        [Fact]
        public void Close_Should_Remove_Window_From_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var window = new Window();

                window.Show();
                Assert.Equal(1, lifetime.Windows.Count);
                window.Close();

                Assert.Empty(lifetime.Windows);
            }
        }
        
        [Fact]
        public void Impl_Closing_Should_Remove_Window_From_OpenWindows()
        {
            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            windowImpl.SetupProperty(x => x.Closed);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);

            var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns(screen1);
            windowImpl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);
            
            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var window = new Window();

                window.Show();
                Assert.Equal(1, lifetime.Windows.Count);
                windowImpl.Object.Closed!();

                Assert.Empty(lifetime.Windows);
            }
        }

        [Fact]
        public void Should_Allow_Canceling_Shutdown_Via_ShutdownRequested_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow.With()))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                
                // Force exit immediately
                Dispatcher.UIThread.Post(Dispatcher.UIThread.ExitAllFrames);
                lifetime.Start(Array.Empty<string>());

                var window = new Window();
                var raised = 0;

                window.Show();

                lifetime.ShutdownRequested += (_, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, new ShutdownRequestedEventArgs());

                Assert.Equal(1, raised);
                Assert.Equal(new[] { window }, lifetime.Windows);
            }
        }
        
        [Fact]
        public void Platform_Shutdown_Without_Deferrals_Should_Close_Windows_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                Setup(lifetime);

                var windowA = new Window();
                var windowB = new Window();
                var closingA = 0;
                var closingB = 0;
                var closedA = 0;
                var closedB = 0;
                var shutdownRequested = 0;
                var exited = 0;
                var replies = new List<bool>();
                var request = new ShutdownRequestedEventArgs(replies.Add);

                windowA.Closing += (_, _) => ++closingA;
                windowB.Closing += (_, _) => ++closingB;
                windowA.Closed += (_, _) => ++closedA;
                windowB.Closed += (_, _) => ++closedB;
                lifetime.Exit += (_, _) => ++exited;
                lifetime.ShutdownRequested += (_, e) =>
                {
                    ++shutdownRequested;
                    Assert.Same(request, e);
                    Assert.Equal(new[] { windowA, windowB }, lifetime.Windows);
                    Assert.Equal(0, closingA + closingB);
                    Assert.Equal(0, closedA + closedB);
                    Assert.Equal(0, exited);
                };

                windowA.Show();
                windowB.Show();

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, request);
                request.EndDeferralRequest();

                Assert.False(request.Cancel);
                Assert.Empty(replies);
                Assert.Empty(lifetime.Windows);
                Assert.False(windowA.IsVisible);
                Assert.False(windowB.IsVisible);
                Assert.Equal(1, closingA);
                Assert.Equal(1, closingB);
                Assert.Equal(1, closedA);
                Assert.Equal(1, closedB);
                Assert.Equal(1, shutdownRequested);
                Assert.Equal(1, exited);
            }
        }

        [Fact]
        public void Platform_Shutdown_Without_Deferrals_Should_Respect_Window_Cancellation()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                Setup(lifetime);

                var window = new Window();
                var closing = 0;
                var closed = 0;
                var shutdownRequested = 0;
                var exited = 0;
                var replies = new List<bool>();
                var request = new ShutdownRequestedEventArgs(replies.Add);

                window.Closing += (_, e) =>
                {
                    ++closing;
                    e.Cancel = true;
                };
                window.Closed += (_, _) => ++closed;
                lifetime.ShutdownRequested += (_, _) => ++shutdownRequested;
                lifetime.Exit += (_, _) => ++exited;
                window.Show();

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, request);
                request.CancelDeferrals();
                request.EndDeferralRequest();

                Assert.True(request.Cancel);
                Assert.Empty(replies);
                Assert.Equal(new[] { window }, lifetime.Windows);
                Assert.True(window.IsVisible);
                Assert.Equal(1, closing);
                Assert.Equal(0, closed);
                Assert.Equal(1, shutdownRequested);
                Assert.Equal(0, exited);
            }
        }

        [Fact]
        public void Non_Native_Shutdown_Request_Should_Reject_Deferrals()
        {
            var request = new ShutdownRequestedEventArgs();

            Assert.Throws<InvalidOperationException>(() => request.GetDeferral());
            Assert.False(request.Cancel);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void Deferred_Shutdown_Should_Wait_For_All_Deferrals_And_Reply_Once(
            bool cancelFirst, bool cancelSecond, bool cancelWindow)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                Setup(lifetime);

                var window = new Window();
                var events = new List<string>();
                var replies = new List<bool>();
                ShutdownRequestedDeferral? first = null;
                ShutdownRequestedDeferral? second = null;
                var request = new ShutdownRequestedEventArgs(shouldTerminate =>
                {
                    events.Add("Reply");
                    replies.Add(shouldTerminate);
                });

                lifetime.ShutdownRequested += (_, e) =>
                {
                    events.Add("ShutdownRequested");
                    first = e.GetDeferral();
                    second = e.GetDeferral();
                };
                window.Closing += (_, e) =>
                {
                    events.Add("Closing");
                    e.Cancel = cancelWindow;
                };
                window.Closed += (_, _) => events.Add("Closed");
                lifetime.Exit += (_, _) => events.Add("Exit");
                window.Show();

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, request);
                request.EndDeferralRequest();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "ShutdownRequested" }, events);
                Assert.Equal(new[] { window }, lifetime.Windows);
                Assert.True(window.IsVisible);
                Assert.Empty(replies);
                Assert.Throws<InvalidOperationException>(() => request.GetDeferral());

                first!.Complete(cancelFirst);
                Assert.Throws<InvalidOperationException>(() => first.Complete(!cancelFirst));
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "ShutdownRequested" }, events);
                Assert.Equal(new[] { window }, lifetime.Windows);
                Assert.True(window.IsVisible);
                Assert.Empty(replies);

                second!.Complete(cancelSecond);
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var shouldTerminate = !cancelFirst && !cancelSecond && !cancelWindow;
                Assert.Equal(new[] { shouldTerminate }, replies);
                if (cancelFirst || cancelSecond)
                {
                    Assert.Equal(new[] { "ShutdownRequested", "Reply" }, events);
                    Assert.Equal(new[] { window }, lifetime.Windows);
                    Assert.True(window.IsVisible);
                }
                else if (cancelWindow)
                {
                    Assert.Equal(new[] { "ShutdownRequested", "Closing", "Reply" }, events);
                    Assert.True(request.Cancel);
                    Assert.Equal(new[] { window }, lifetime.Windows);
                    Assert.True(window.IsVisible);
                }
                else
                {
                    Assert.Equal(new[] { "ShutdownRequested", "Closing", "Closed", "Exit", "Reply" }, events);
                    Assert.Empty(lifetime.Windows);
                    Assert.False(window.IsVisible);
                }

                Assert.Throws<InvalidOperationException>(() => second.Complete(!cancelSecond));
                Assert.Equal(new[] { shouldTerminate }, replies);
            }
        }

        [Fact]
        public void Inline_Deferral_Completion_Should_Wait_For_All_Shutdown_Handlers()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(lifetimeEvents.Object);
                Setup(lifetime);

                var window = new Window();
                var events = new List<string>();
                var request = new ShutdownRequestedEventArgs(shouldTerminate =>
                {
                    Assert.True(shouldTerminate);
                    events.Add("Reply");
                });

                lifetime.ShutdownRequested += (_, e) =>
                {
                    events.Add("FirstHandler");
                    e.GetDeferral().Complete();
                };
                lifetime.ShutdownRequested += (_, e) =>
                {
                    events.Add("SecondHandler");
                    e.GetDeferral().Complete();
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                    Assert.Equal(new[] { "FirstHandler", "SecondHandler" }, events);
                    Assert.Equal(new[] { window }, lifetime.Windows);
                    Assert.True(window.IsVisible);
                };
                window.Closing += (_, _) => events.Add("Closing");
                window.Closed += (_, _) => events.Add("Closed");
                lifetime.Exit += (_, _) => events.Add("Exit");
                window.Show();

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, request);

                Assert.Equal(new[] { "FirstHandler", "SecondHandler" }, events);
                Assert.Equal(new[] { window }, lifetime.Windows);

                request.EndDeferralRequest();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "FirstHandler", "SecondHandler", "Closing", "Closed", "Exit", "Reply" }, events);
                Assert.Empty(lifetime.Windows);
                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void MainWindow_Closed_Shutdown_Should_Be_Cancellable()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var mainWindow = new Window();

                mainWindow.Show();

                lifetime.MainWindow = mainWindow;

                var window = new Window();

                window.Show();

                var raised = 0;
                
                lifetime.ShutdownRequested += (_, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                mainWindow.Close();
                
                Assert.Equal(1, raised);
                Assert.False(hasExit);
            }
        }
        
        [Fact]
        public void LastWindow_Closed_Shutdown_Should_Be_Cancellable()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();
                
                var raised = 0;
                
                lifetime.ShutdownRequested += (_, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.Equal(1, raised);
                Assert.False(hasExit);
            }
        }
        
        [Fact]
        public void TryShutdown_Cancellable_By_Preventing_Window_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                lifetime.Exit += (_, _) => Assert.Fail("lifetime.Exit was called.");
                Dispatcher.UIThread.ShutdownStarted += UiThreadOnShutdownStarted;

                static void UiThreadOnShutdownStarted(object? sender, EventArgs e)
                {
                    Assert.Fail("Dispatcher.UIThread.ShutdownStarted was called.");
                }

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();
                
                var raised = 0;

                windowA.Closing += (_, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                lifetime.TryShutdown();

                Assert.Equal(1, raised);

                Dispatcher.UIThread.ShutdownStarted -= UiThreadOnShutdownStarted;
            }
        }
        
        [Fact]
        public void Shutdown_NotCancellable_By_Preventing_Window_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow.With()))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var hasExit = false;
                var closingRaised = 0;
                var closedRaised = 0;

                lifetime.Exit += (_, _) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Closing += (_, e) =>
                {
                    e.Cancel = true;
                    ++closingRaised;
                };
                windowA.Closed += (_, e) =>
                {
                    ++closedRaised;
                };

                lifetime.Shutdown();

                Assert.Equal(1, closingRaised);
                Assert.Equal(1, closedRaised);
                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Shutdown_Doesnt_Raise_Shutdown_Requested()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                Setup(lifetime);

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;
                
                var raised = 0;

                lifetime.ShutdownRequested += (_, _) =>
                {
                    ++raised;
                };

                lifetime.Shutdown();

                Assert.Equal(0, raised);
                Assert.True(hasExit);
            }
        }

        [Fact]
        public void SetupWithClassicDesktopLifetime_Should_Subscribe_To_Platform_ShutdownRequested()
        {
            var lifetimeEvents = new Mock<IPlatformLifetimeEventsImpl>();
            ClassicDesktopStyleApplicationLifetime? lifetime = null;

            CreateAppBuilder(lifetimeEvents.Object).SetupWithClassicDesktopLifetime(
                [],
                l => lifetime = (ClassicDesktopStyleApplicationLifetime)l);

            Assert.NotNull(lifetime);

            using (lifetime)
            {
                var window = new Window();
                window.Show();

                var raised = 0;

                lifetime.ShutdownRequested += (_, e) =>
                {
                    e.Cancel = true;
                    ++raised;
                };

                lifetimeEvents.Raise(x => x.ShutdownRequested += null, new ShutdownRequestedEventArgs());

                Assert.Equal(1, raised);
                Assert.Equal([window], lifetime.Windows);
            }
        }

        [Fact]
        public void SetupWithClassicDesktopLifetime_Should_Not_Raise_Startup()
        {
            var frameworkInitCalled = false;
            var lifetimeBuilderCalled = false;
            var startupRaised = false;

            CreateAppBuilder(onFrameworkInitializationCompleted: () => frameworkInitCalled = true)
                .SetupWithClassicDesktopLifetime(
                    ["foo", "bar"],
                    l =>
                    {
                        lifetimeBuilderCalled = true;
                        l.Startup += (_, _) => startupRaised = true;
                    });

            Assert.True(frameworkInitCalled);
            Assert.True(lifetimeBuilderCalled);
            Assert.False(startupRaised);
        }

        [Fact]
        public void Start_After_SetupWithClassicDesktopLifetime_Should_Not_Raise_Startup_Twice()
        {
            ClassicDesktopStyleApplicationLifetime? lifetime = null;
            var raised = 0;

            CreateAppBuilder().SetupWithClassicDesktopLifetime(
                [],
                l =>
                {
                    lifetime = (ClassicDesktopStyleApplicationLifetime)l;
                    l.Startup += (_, _) => ++raised;
                });

            Assert.NotNull(lifetime);

            using (lifetime)
            {
                Assert.Equal(0, raised);

                Dispatcher.UIThread.Post(Dispatcher.UIThread.ExitAllFrames);
                lifetime.Start([]);

                Assert.Equal(1, raised);
            }
        }

        private static void Setup(ClassicDesktopStyleApplicationLifetime lifetime)
        {
            ISetupApplicationLifetime setupLifetime = lifetime;
            setupLifetime.BeforeAppInit();
            setupLifetime.AfterAppInit();
        }

        private static AppBuilder CreateAppBuilder(
            IPlatformLifetimeEventsImpl? platformLifetimeEvents = null,
            Action? onFrameworkInitializationCompleted = null)
        {
            AppBuilder.ResetSetupForUnitTests();

            return AppBuilder.Configure(() => new SetupTestApplication(onFrameworkInitializationCompleted))
                .UseRuntimePlatformSubsystem(() => { })
                .UseRenderingSubsystem(() => { })
                .UseTextShapingSubsystem(() => { })
                .UseWindowingSubsystem(() =>
                {
                    if (platformLifetimeEvents is not null)
                        AvaloniaLocator.CurrentMutable.Bind<IPlatformLifetimeEventsImpl>().ToConstant(platformLifetimeEvents);
                });
        }

        private sealed class SetupTestApplication(Action? onFrameworkInitializationCompleted)
            : UnitTestApplication(TestServices.StyledWindow)
        {
            private bool _servicesRegistered;

            public override void RegisterServices()
            {
                if (_servicesRegistered)
                    return;

                _servicesRegistered = true;
                base.RegisterServices();
            }

            public override void OnFrameworkInitializationCompleted()
            {
                base.OnFrameworkInitializationCompleted();
                onFrameworkInitializationCompleted?.Invoke();
            }
        }
    }
}
