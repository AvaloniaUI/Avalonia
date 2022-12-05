using Avalonia;
using Avalonia.Platform;
using MiniMvvm;

namespace ControlCatalog.ViewModels;
#nullable enable

public class PlatformInformationViewModel : ViewModelBase
{
    public PlatformInformationViewModel()
    {
        var runtimeInfo = AvaloniaLocator.Current.GetService<IRuntimePlatform>()?.GetRuntimeInfo();

        if (runtimeInfo is { } info)
        {
            if (info.IsBrowser)
            {
                if (info.IsDesktop)
                {
                    PlatformInfo = "Platform: Desktop (browser)";
                }
                else if (info.IsMobile)
                {
                    PlatformInfo = "Platform: Mobile (browser)";
                }
                else
                {
                    PlatformInfo = "Platform: Unknown (browser) - please report";
                }
            }
            else
            {
                if (info.IsDesktop)
                {
                    PlatformInfo = "Platform: Desktop (native)";
                }
                else if (info.IsMobile)
                {
                    PlatformInfo = "Platform: Mobile (native)";
                }
                else
                {
                    PlatformInfo = "Platform: Unknown (native) - please report";
                }
            }
        }
        else
        {
            
        }
    }
    
    public string PlatformInfo { get; }
}
