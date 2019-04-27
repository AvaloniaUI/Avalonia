// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ApplicationTests
    {
        [Fact]
        public void Should_Exit_After_MainWindow_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var hasExit = false;

                Application.Current.Exit += (s, e) => hasExit = true;

                var mainWindow = new Window();

                mainWindow.Show();

                Application.Current.MainWindow = mainWindow;

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
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

                var hasExit = false;

                Application.Current.Exit += (s, e) => hasExit = true;

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
        public void Should_Only_Exit_On_Explicit_Exit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var hasExit = false;

                Application.Current.Exit += (s, e) => hasExit = true;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(hasExit);

                windowB.Close();

                Assert.False(hasExit);

                Application.Current.Shutdown();

                Assert.True(hasExit);
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

                Application.Current.Shutdown();

                Assert.Empty(Application.Current.Windows);
            }
        }

        [Fact]
        public void Throws_ArgumentNullException_On_Run_If_MainWindow_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Assert.Throws<ArgumentNullException>(() => { Application.Current.Run(null); });
            }
        }

        [Fact]
        public void Raises_ResourcesChanged_When_Event_Handler_Added_After_Resources_Has_Been_Accessed()
        {
            // Test for #1765.
            using (UnitTestApplication.Start())
            {
                var resources = Application.Current.Resources;
                var raised = false;

                Application.Current.ResourcesChanged += (s, e) => raised = true;
                resources["foo"] = "bar";

                Assert.True(raised);
            }
        }

        [Fact]
        public void Throws_InvalidOperationException_On_Run_When_Application_Is_Already_Running()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                Application.Current.Startup += (s, e) =>
                {
                    Assert.Throws<InvalidOperationException>(() => { Application.Current.Run(); });
                };

                Application.Current.Run();               
            }
        }

        [Fact]
        public void Should_Set_ExitCode_After_Shutdown()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                Application.Current.Shutdown(1337);

                var exitCode = Application.Current.Run();

                Assert.Equal(1337, exitCode);
            }
        }
    }
}
