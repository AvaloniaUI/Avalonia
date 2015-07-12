namespace TestApplication
{
    using Perspex;
	using Perspex.Themes.Default;

#if PERSPEX_CAIRO
	using Perspex.Cairo;
#else
	using Perspex.Direct2D1;
#endif

#if PERSPEX_GTK
	using Perspex.Gtk;
#else
    using Perspex.Win32;
#endif

    public class App : Application
    {
        public App()
            : base(new DefaultTheme())
        {
#if PERSPEX_CAIRO
            CairoPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif

#if PERSPEX_GTK
			GtkPlatform.Initialize();
#else
			Win32Platform.Initialize();
#endif
        }
    }
}
