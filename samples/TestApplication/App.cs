// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Controls;
using Perspex.Media;
using Perspex.Styling;
using Perspex.Themes.Default;

namespace TestApplication
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
            InitializeSubsystems((int)Environment.OSVersion.Platform);            
            Styles = new DefaultTheme();
            Styles.Add(new SampleTabStyle());
            var testStyle = new Style(x => x.OfType<Button>())
            {
                Setters = new[]
            {
                new Setter(Button.BackgroundProperty, Brushes.Red),
            }
            };

            Styles.Add(testStyle);
        }
    }
}
