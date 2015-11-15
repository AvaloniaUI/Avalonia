// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Controls;
using Perspex.Controls.Templates;
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
            DataTemplates = new DataTemplates
            {
                new FuncTreeDataTemplate<Node>(
                    x => new TextBlock {Text = x.Name},
                    x => x.Children,
                    x => true),
            };
        }


    }
}
