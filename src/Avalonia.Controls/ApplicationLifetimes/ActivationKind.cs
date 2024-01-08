namespace Avalonia.Controls.ApplicationLifetimes;

public enum ActivationKind
{
    /// <summary>
    /// When the application is passed a URI to open.
    /// </summary>
    OpenUri = 20, 
    
    /// <summary>
    /// When the application is asked to reopen.
    /// An example of this is on MacOS when all the windows are closed,
    /// application continues to run in the background and the user clicks
    /// the application's dock icon. 
    /// </summary>
    Reopen = 30,
    
    /// <summary>
    /// When the application enters or leaves a background state.
    /// An example is when on MacOS the user hides or shows and application (not window),
    /// or when a browser application switchs tabs or when a mobile applications goes into
    /// the background.
    /// </summary>
    Background = 40
}
