using System;
using System.Linq;
using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
using Perspex.Logging.Serilog;
using Serilog;

namespace ControlCatalog
{
	// Eventually we should move this into a PCL library so we can access
	// from mobile platforms
	//
    class App : Application
    {
        public App()
        {
            RegisterServices();
        }
	}
}
