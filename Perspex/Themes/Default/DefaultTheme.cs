using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    public class DefaultTheme : Styles
    {
        public DefaultTheme()
        {
            this.Add(new ButtonStyle());
        }
    }
}
