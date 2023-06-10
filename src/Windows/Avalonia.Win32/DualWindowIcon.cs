using System.IO;
using Avalonia.Controls;

namespace Avalonia.Win32;

/// <summary>
/// <para>Represents two icons for use by a single <see cref="Window"/>. The "small" icon will appear in the title bar, while the "big" icon will appear in the taskbar.</para>
/// <para>Both icons can individually be .ICO files containing multiple image resolutions for display at different DPIs.</para>
/// </summary>
public class DualWindowIcon : WindowIcon
{
    public DualWindowIcon(Stream smallIcon, Stream bigIcon) : base(new IconImpl(smallIcon, bigIcon))
    {
    }
}
