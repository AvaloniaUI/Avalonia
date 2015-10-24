using Perspex;
using Perspex.Markup.Xaml;
using Perspex.Themes.Default;

namespace XamlTestApplication
{
    public abstract class XamlTestApp : Application
    {
        protected abstract void RegisterPlatform();

        public XamlTestApp()
        {
            RegisterServices();
            RegisterPlatform();
            Styles = new Perspex.Styling.Styles();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var loader = new PerspexXamlLoader();
            loader.Load(typeof(XamlTestApp), this);
        }
    }
}
