using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface ISingleViewApplicationLifetime : IApplicationLifetime
    {
        Control? MainView { get; set; }
    }
}
