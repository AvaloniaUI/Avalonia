using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Foundation;
using UIKit;

namespace Avalonia
{
    public class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType().Assembly))
        {

        }
    }
}