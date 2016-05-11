using Avalonia;


namespace ControlCatalog
{
    // Eventually we should move this into a PCL library so we can access
    // from mobile platforms
    //
    public class App : Application
    {
        public App()
        {
            RegisterServices();
        }
    }
}
