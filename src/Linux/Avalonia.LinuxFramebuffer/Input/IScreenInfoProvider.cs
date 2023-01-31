namespace Avalonia.LinuxFramebuffer.Input;
/// <summary>
/// Screen Info Provider base interface 
/// </summary>
public interface IScreenInfoProvider
{
    /// <summary>
    /// Current screen size 
    /// </summary>
    Size ScaledSize { get; }
}

