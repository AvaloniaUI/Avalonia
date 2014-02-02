using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Media;

namespace Perspex.Themes.Default
{
    public class ButtonStyle : Styles
    {
        public ButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.Select<Button>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(Button.BorderThicknessProperty, 2.0),
                        new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff000000)),
                    },
                },
                new Style(x => x.Select<Button>().Class(":mouseover"))
                {
                    Setters = new[]
                    {
                        new Setter (Button.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter (Button.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.Select<Button>().Class(":pressed"))
                {
                    Setters = new[]
                    {
                        new Setter (Button.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                        new Setter (Button.BorderBrushProperty, new SolidColorBrush(0xff2c628b)),
                    },
                },
            });
        }
    }
}
