namespace Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// An interface for lifetimes that need to execute extra code before and after initialization.
/// </summary>
internal interface ISetupApplicationLifetime
{
    /// <summary>
    /// Called before anything is initialized: platforms, rendering, app, etc. aren't available yet.
    /// </summary>
    void BeforeAppInit();

    /// <summary>
    /// Called after the app has been initialized.
    /// </summary>
    void AfterAppInit();
}
