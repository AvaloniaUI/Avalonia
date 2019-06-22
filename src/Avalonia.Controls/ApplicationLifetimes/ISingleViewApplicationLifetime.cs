namespace Avalonia.Controls.ApplicationLifetimes
{
    public interface ISingleViewApplicationLifetime : IApplicationLifetime
    {
        Control MainView { get; set; }
    }
}
