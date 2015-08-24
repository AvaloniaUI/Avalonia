namespace XamlTestApplication
{
    using System;
    using Perspex;
    using Perspex.Themes.Default;

    public class App : Application
    {
        public App()
        {
            this.RegisterServices();
            this.InitializeSubsystems((int)Environment.OSVersion.Platform);
            this.Styles = new DefaultTheme();
        }
    }
}
