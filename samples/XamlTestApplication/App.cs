// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Themes.Default;

namespace XamlTestApplication
{
    public class App : XamlTestApp
    {
        protected override void RegisterPlatform()
        {
            InitializeSubsystems((int)Environment.OSVersion.Platform);
        }
    }
}
