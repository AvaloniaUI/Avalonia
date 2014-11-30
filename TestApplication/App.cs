namespace TestApplication
{
    using Perspex;
    using Perspex.Direct2D1;
    using Perspex.Themes.Default;
    using Perspex.Win32;

    public class App : Application
    {
        public App()
            : base(new DefaultTheme())
        {
            this.RegisterServices();
            Direct2D1Platform.Initialize();
            Win32Platform.Initialize();
        }
    }
}
