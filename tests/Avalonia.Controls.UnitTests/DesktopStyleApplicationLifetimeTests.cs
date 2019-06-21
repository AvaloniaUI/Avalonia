using System;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    
    public class DesktopStyleApplicationLifetimeTests
    {
        [Fact]
        public void Should_Set_ExitCode_After_Shutdown()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var lifetime = new ClassicDesktopStyleApplicationLifetime(Application.Current);
                Dispatcher.UIThread.InvokeAsync(() => lifetime.Shutdown(1337));
                lifetime.Shutdown(1337);

                var exitCode = lifetime.Start(Array.Empty<string>());

                Assert.Equal(1337, exitCode);
            }
        }
        
        
        [Fact]
        public void Should_Close_All_Remaining_Open_Windows_After_Explicit_Exit_Call()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var windows = new List<Window> { new Window(), new Window(), new Window(), new Window() };

                foreach (var window in windows)
                {
                    window.Show();
                }
                new ClassicDesktopStyleApplicationLifetime(Application.Current).Shutdown();

                Assert.Empty(Application.Current.Windows);
            }
        }
        
        [Fact]
        public void Should_Only_Exit_On_Explicit_Exit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var lifetime = new ClassicDesktopStyleApplicationLifetime(Application.Current);
                lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

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
            {
                var lifetime =  new ClassicDesktopStyleApplicationLifetime(Application.Current);
                lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

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
            {
                var lifetime = new ClassicDesktopStyleApplicationLifetime(Application.Current);
                lifetime.ShutdownMode = ShutdownMode.OnLastWindowClose;

                var hasExit = false;

                lifetime.Exit += (s, e) => hasExit = true;

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
    }
    
}
