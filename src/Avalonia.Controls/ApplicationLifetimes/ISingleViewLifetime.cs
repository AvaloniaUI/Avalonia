namespace Avalonia.Controls.ApplicationLifetimes
{
    public interface ISingleViewLifetime : IApplicationLifetime
    {
        Control MainView { get; set; }
    }
}
