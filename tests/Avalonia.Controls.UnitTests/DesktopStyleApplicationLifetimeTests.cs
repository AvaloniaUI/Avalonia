using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    
    public class DesktopStyleApplicationLifetimeTests
    {
        IDispatcherImpl CreateDispatcherWithInstantMainLoop()
        {
            var mock = new Mock<IControlledDispatcherImpl>();
            mock.Setup(x => x.RunLoop(It.IsAny<CancellationToken>()))
                .Callback(() => Dispatcher.UIThread.ExitAllFrames());
            mock.Setup(x => x.CurrentThreadIsLoopThread).Returns(true);
            return mock.Object;
        }
        
        [Fact]
        public void Should_Set_ExitCode_After_Shutdown()
        {
            using (UnitTestApplication.Start(new TestServices(dispatcherImpl: new ManagedDispatcherImpl(null))))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())    
            {
                lifetime.SetupCore(Array.Empty<string>());

                Dispatcher.UIThread.Post(() => lifetime.Shutdown(1337));
                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(1337, exitCode);
            }
        }
        
        
        [Fact]
        public void Should_Close_All_Remaining_Open_Windows_After_Explicit_Exit_Call()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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
        public void Should_Exit_After_Last_Window_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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

            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.SetupCore(Array.Empty<string>());

                var window = new Window();

                window.Show();
                Assert.Equal(1, lifetime.Windows.Count);
                windowImpl.Object.Closed();

                Assert.Empty(lifetime.Windows);
            }
        }

        [Fact]
        public void Should_Allow_Canceling_Shutdown_Via_ShutdownRequested_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow.With(dispatcherImpl: new ManagedDispatcherImpl(null))))
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
        public void MainWindow_Closed_Shutdown_Should_Be_Cancellable()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

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
                lifetime.SetupCore(Array.Empty<string>());

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

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
                Assert.False(hasExit);
            }
        }
        
        [Fact]
        public void Shutdown_NotCancellable_By_Preventing_Window_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow.With(dispatcherImpl: CreateDispatcherWithInstantMainLoop())))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.SetupCore(Array.Empty<string>());

                var hasExit = false;

                lifetime.Exit += (_, _) => hasExit = true;

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

                lifetime.Shutdown();

                Assert.Equal(1, raised);
                Assert.True(hasExit);
            }
        }
        
        [Fact]
        public void Shutdown_Doesnt_Raise_Shutdown_Requested()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using(var lifetime = new ClassicDesktopStyleApplicationLifetime())
            {
                lifetime.SetupCore(Array.Empty<string>());

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
    }
}
