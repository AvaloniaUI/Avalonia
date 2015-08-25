#if PERSPEX_GTK
using Perspex.Gtk;
#endif

namespace BingSearchApp
{
    using Glass;
    using OmniXaml.AppServices.Mvvm;
    using OmniXaml.AppServices.NetCore;
    using Perspex;
    using Perspex.Controls;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Xaml.Desktop;
    using Properties;
    using ViewModels;

    class Program
    {
        static void Main(string[] args)
        {            
            System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            App application = new App
            {
                DataTemplates = new DataTemplates
                {
                    new TreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children,
                        x => true),
                },
            };

            var appKey = Settings.Default.AppKey;
            if (appKey == null)
            {
                //Current.Shutdown();
            }

            var typeFactory = new PerspexInflatableTypeFactory();

            var viewFactory = new ViewFactory(typeFactory);
            viewFactory.RegisterViews(ViewRegistration.FromTypes(Assemblies.AppDomainAssemblies.AllExportedTypes()));

            var window = viewFactory.GetWindow("Main");
            window.SetViewModel(new MainViewModel(new BingSearchService(appKey)));
            window.Show();
            Application.Current.Run((ICloseable)window);
        }
    }
}
