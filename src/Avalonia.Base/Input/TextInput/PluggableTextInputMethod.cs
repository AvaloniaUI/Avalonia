using Avalonia.Interactivity;

namespace Avalonia.Input.TextInput;

public abstract class PluggableTextInputMethod
{
    internal TextInputMethodAdapter Adapter { get; }
    
    public PluggableTextInputMethod()
    {
        Adapter = new(this);
    }

    public virtual void SetClient(TextInputMethodClient? client)
    {
    }

    public virtual void SetOptions(TextInputOptions options)
    {
    }
    
    internal class TextInputMethodAdapter(PluggableTextInputMethod method) : ITextInputMethodImpl
    {
        public void SetClient(TextInputMethodClient? client)
        {
            method.SetClient(client);
        }

        public void SetCursorRect(Rect rect)
        {
            // No-op
        }

        public void SetOptions(TextInputOptions options) => method.SetOptions(options);

        public void Reset()
        {
            // Implementations should be subscribing to reset event manually
        }
    }
    
    /// <summary>
    /// Defines the <see cref="TextInputMethodRequestedEvent"/> event.
    /// </summary>
    public static readonly RoutedEvent<TextInputMethodClientRequestedEventArgs> TextInputMethodRequestedEvent =
        RoutedEvent.Register<InputElement, TextInputMethodClientRequestedEventArgs>(
            nameof(PluggableTextInputMethodRequestedEventArgs),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
}

public class PluggableTextInputMethodRequestedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Set this property to a valid pluggable text input method to enable its usage with the input system
    /// </summary>
    public PluggableTextInputMethod? InputMethod { get; set; }
}