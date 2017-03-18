using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avalonia.iOSTestApplication
{
    class SimpleControl : ContentControl
    {
        public SimpleControl()
        {
            Content = new Button() {Content = "WAT"};
            MinWidth = 100;
            MinHeight = 200;
            Background = Brushes.CadetBlue;
        }
    }
}