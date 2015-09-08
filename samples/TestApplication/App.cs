// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Themes.Default;

namespace TestApplication
{
    public class App : Application
    {
        public App()
        {
            this.RegisterServices();
            this.InitializeSubsystems((int)Environment.OSVersion.Platform);
            this.Styles = new DefaultTheme();
        }
    }
}
