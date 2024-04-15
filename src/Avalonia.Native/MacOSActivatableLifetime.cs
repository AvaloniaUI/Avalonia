using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;

namespace Avalonia.Native;

#nullable enable

internal class MacOSActivatableLifetime : ActivatableLifetimeBase
{
    public override bool TryLeaveBackground()
    {
        var nativeApplicationCommands = AvaloniaLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.ShowApp();

        return true;
    }

    public override bool TryEnterBackground()
    {
        var nativeApplicationCommands = AvaloniaLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.HideApp();

        return true;
    }
}
