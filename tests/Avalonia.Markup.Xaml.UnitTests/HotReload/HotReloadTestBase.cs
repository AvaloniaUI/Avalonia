using Avalonia.Controls;
using Avalonia.Markup.Xaml.HotReload;

namespace Avalonia.Markup.Xaml.UnitTests.HotReload
{
    public class TestControl : UserControl
    {
    }
    
    public class HotReloadTestBase : XamlTestBase
    {
        protected (T Original, T Modified) ParseAndApplyHotReload<T>(string xaml, string modifiedXaml)
        {
            var original = AvaloniaRuntimeXamlLoader.Parse<T>(xaml);
            var modified = AvaloniaRuntimeXamlLoader.Parse<T>(modifiedXaml);
            
            var actions = HotReloadDiffer.Diff<T>(xaml, modifiedXaml);

            foreach (var action in actions)
            {
                action.Apply(original);
            }

            return (original, modified);
        }
    }
}
