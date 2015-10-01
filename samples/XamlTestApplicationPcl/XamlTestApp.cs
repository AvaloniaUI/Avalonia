using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex;
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
            Styles = new DefaultTheme();
        }
    }
}
