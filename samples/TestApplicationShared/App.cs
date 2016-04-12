// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml;
using Perspex.Styling;
using Perspex.Themes.Default;

namespace TestApplication
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();

#if !__IOS__	// IOS Startup flow is a bit different and cannot use this
			RegisterPlatformCallback(PlatformInitialization);
#endif

			InitializeSubsystems((int)Environment.OSVersion.Platform);

			Styles.Add(new DefaultTheme());

            var loader = new PerspexXamlLoader();
            var baseLight = (IStyle)loader.Load(
                new Uri("resm:Perspex.Themes.Default.Accents.BaseLight.xaml?assembly=Perspex.Themes.Default"));
            Styles.Add(baseLight);

            Styles.Add(new SampleTabStyle());
            DataTemplates = new DataTemplates
            {
                new FuncTreeDataTemplate<Node>(
                    x => new TextBlock {Text = x.Name},
                    x => x.Children),
            };
        }

		protected virtual void PlatformInitialization()
		{
			// default behavior
			InitializeSubsystems((int)Environment.OSVersion.Platform);
		}
	}
}
