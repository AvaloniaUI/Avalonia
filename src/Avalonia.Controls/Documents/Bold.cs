using Avalonia.Media;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// Bold element - markup helper for indicating bolded content.
    /// Equivalent to a Span with FontWeight property set to FontWeights.Bold.
    /// Can contain other inline elements.
    /// </summary>
    public sealed class Bold : Span
    {
        static Bold()
        {
            FontWeightProperty.OverrideDefaultValue<Bold>(FontWeight.Bold);
        }
    }
}
