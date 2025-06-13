using System;
using System.Linq;
using UIKit;

namespace ControlCatalog.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            var delegateType = args.Contains("-PlatformView", StringComparer.InvariantCultureIgnoreCase) ?
                typeof(PlatformViewAppDelegate) : typeof(SingleViewAppDelegate);

            UIApplication.Main(args, null, delegateType);
        }
    }
}
