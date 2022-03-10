namespace Avalonia.Input.TextInput;

public class TextInputOptions
{
    /// <summary>
    /// Defines the <see cref="ContentType"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextInputContentType> ContentTypeProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, AvaloniaObject, TextInputContentType>(
            "ContentType",
            defaultValue: TextInputContentType.Normal,
            inherits: true);
    
    
    /// <summary>
    /// Sets the value of the attached <see cref="ContentTypeProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetContentType(AvaloniaObject avaloniaObject, TextInputContentType value)
    {
        avaloniaObject.SetValue(ContentTypeProperty, value);
            
            
    }
    
    /// <summary>
    /// Gets the value of the attached <see cref="ContentTypeProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <returns>The font family.</returns>
    public static TextInputContentType GetContentType(AvaloniaObject avaloniaObject)
    {
        return avaloniaObject.GetValue(ContentTypeProperty);
    }
    
}
