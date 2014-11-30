namespace TestApplication
{
    using Perspex;
    using Perspex.Cairo;
    using Perspex.Direct2D1;
    using Perspex.Themes.Default;
    using Perspex.Win32;

    public class App : Application
    {
        public App()
            : base(new DefaultTheme())
        {
            this.RegisterServices();
#if PERSPEX_CAIRO
            CairoPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif
            Win32Platform.Initialize();
        }
    }
}
