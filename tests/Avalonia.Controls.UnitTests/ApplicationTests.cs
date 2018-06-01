// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.UnitTests
{
    using Avalonia.UnitTests;

    using Xunit;

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
    }
}
