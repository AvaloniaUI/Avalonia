// -----------------------------------------------------------------------
// <copyright file="App.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient
{
    using System;
    using Views;
    using Perspex;
    using Perspex.Controls;
    using Perspex.Diagnostics;
    using Perspex.Themes.Default;
    using ReactiveUI;

    public class App : Application
    {
        public App()
        {
            this.RegisterServices();
            this.InitializeDataTemplates();

            this.InitializeSubsystems((int)Environment.OSVersion.Platform);
            this.Styles = new DefaultTheme();
        }

        public static void AttachDevTools(Window window)
        {
#if DEBUG
            DevTools.Attach(window);
#endif
        }

        private static void Main(string[] args)
        {
            var app = new App();
            var window = new MainWindow();
            window.Show();
            app.Run(window);
        }

        private void InitializeDataTemplates()
        {
            this.DataTemplates.Add(new ViewLocator<ReactiveObject>());
        }
    }
}
