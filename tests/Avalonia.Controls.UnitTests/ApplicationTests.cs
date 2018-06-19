// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                Application.Current.ExitMode = ExitMode.OnMainWindowClose;

                var mainWindow = new Window();

                mainWindow.Show();

                Application.Current.MainWindow = mainWindow;

                var window = new Window();

                window.Show();

                mainWindow.Close();

                Assert.True(Application.Current.IsExiting);
            }
        }

        [Fact]
        public void Should_Exit_After_Last_Window_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.ExitMode = ExitMode.OnLastWindowClose;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(Application.Current.IsExiting);

                windowB.Close();

                Assert.True(Application.Current.IsExiting);
            }
        }

        [Fact]
        public void Should_Only_Exit_On_Explicit_Exit()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.ExitMode = ExitMode.OnExplicitExit;

                var windowA = new Window();

                windowA.Show();

                var windowB = new Window();

                windowB.Show();

                windowA.Close();

                Assert.False(Application.Current.IsExiting);

                windowB.Close();

                Assert.False(Application.Current.IsExiting);

                Application.Current.Exit();

                Assert.True(Application.Current.IsExiting);
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

                Application.Current.Exit();

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
    }
}