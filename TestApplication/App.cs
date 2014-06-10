namespace TestApplication
{
    using Perspex;
    using Perspex.Direct2D1;
    using Perspex.Themes.Default;

    public class App : Application
    {
        public App()
        {
            RegisterPortableServices();
            Direct2D1Platform.Initialize();
            this.Styles = new DefaultTheme();
        }
    }
}
