using Avalonia.Data;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime
{
    public interface IAvaloniaXamlIlValuePriorityProvider
    {
        BindingPriority GetValuePriority();
    }
}
