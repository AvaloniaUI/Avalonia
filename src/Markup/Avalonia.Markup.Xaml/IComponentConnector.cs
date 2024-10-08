namespace Avalonia.Markup.Xaml;

public interface IComponentConnector
{
    /// <summary>
    /// Loads the compiled page of a component.
    /// </summary>
    void InitializeComponent();
    
    /// <summary>
    /// Attaches names to compiled content.
    /// </summary>
    /// <param name="connectionId">An identifier token to distinguish calls.</param>
    /// <param name="target">The target to connect names to.</param>
    void Connect (int connectionId, object target);
}
