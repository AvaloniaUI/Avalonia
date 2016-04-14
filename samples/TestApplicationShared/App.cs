// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml;
using Perspex.Styling;
using Perspex.Themes.Default;
using Perspex.Diagnostics;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;

namespace TestApplication
{
    public class App : Application
    {
        public App()
        {
			// TODO: I believe this has to happen before we select sub systems. Can we
			// move this safely into Application itself? Is there anything in here
			// that is platform specific??
			//
            RegisterServices();
        }

		public void Run()
		{
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

			MainWindow.RootNamespace = "TestApplication";
			var wnd = MainWindow.Create();
			DevTools.Attach(wnd);

			Run(wnd);
		}
	}
}
