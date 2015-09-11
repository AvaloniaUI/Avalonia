using Perspex.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls.Documents
{
    public class Run : Inline
    {
        public string Text
        {
            get; set;
        }

        public SolidColorBrush Foreground
        {
            get;
            set;
        }

        public FontWeight FontWeight
        {
            get;
            set;
        } = FontWeight.Normal;

        public double FontSize
        {
            get;
            set;
        } = 14;
    }
}
