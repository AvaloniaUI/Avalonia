using System;
using Avalonia.Input.Platform;
using Avalonia.Logging;

namespace Avalonia.Controls.Utils;

internal static class PrimarySelectionHelper
{
    /// <summary>
    /// Publishes text to the primary selection clipboard, if available. Failures are logged.
    /// The text is only realized on platforms supporting the primary selection.
    /// </summary>
    public static async void PublishText(Control source, Func<string?> textFactory)
    {
        if (TopLevel.GetTopLevel(source)?.TryGetClipboard(ClipboardType.PrimarySelection) is not { } primarySelection)
            return;

        var text = textFactory();
        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            await primarySelection.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)
                ?.Log(source, "Failed to write text to primary selection: {Error}", ex);
        }
    }
}
