using Tmds.DBus.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

/// <summary>Pixel rectangle in the coordinate system reported by Maliit.</summary>
internal readonly record struct InputArea(int X, int Y, int Width, int Height);

/// <summary>Immutable focus state. Sensitive text is removed again at the transport boundary.</summary>
internal sealed record MaliitWidgetState(TextInputOptions Options, string SurroundingText, int Cursor, int Anchor,
    ulong WindowId, InputArea CursorRectangle, bool Sensitive = false)
{
    public bool IsSensitive => Sensitive || Options.IsSensitive || Options.ContentType is TextInputContentType.Password or TextInputContentType.Pin;
    public string SelectedText => IsSensitive ? "" : SurroundingText[Math.Clamp(Math.Min(Cursor, Anchor), 0, SurroundingText.Length)..Math.Clamp(Math.Max(Cursor, Anchor), 0, SurroundingText.Length)];

    /// <summary>Matches Maliit MInputContext::getStateInformation and Qt InputMethodHint.</summary>
    // https://doc.qt.io/qt-6/qt.html#InputMethodHint-enum
    public Dictionary<string, VariantValue> ToProperties(bool focused = true)
    {
        var input = Options;
        var suggestions = input.ShowSuggestions ?? true;
        var hints = (!input.AutoCapitalization ? 0x4 : 0) | (!suggestions ? 0x40 : 0) | (input.Multiline ? 0x400 : 0);
        hints |= input.ContentType switch
        {
            TextInputContentType.Number => 0x20000,
            TextInputContentType.Digits or TextInputContentType.Pin => 0x10000,
            TextInputContentType.Email => 0x200000,
            TextInputContentType.Url => 0x400000,
            _ => 0
        };
        if (input.Lowercase) hints |= 0x20;
        if (input.Uppercase) hints |= 0x10;
        if (IsSensitive) hints |= 0x1 | 0x2 | 0x4 | 0x40;
        var properties = new Dictionary<string, VariantValue>
        {
            ["focusState"] = focused,
            ["contentType"] = input.ContentType switch
            {
                TextInputContentType.Number or TextInputContentType.Digits or TextInputContentType.Pin => 1,
                TextInputContentType.Email => 3,
                TextInputContentType.Url => 4,
                _ => 0
            },
            ["hiddenText"] = IsSensitive,
            ["predictionEnabled"] = !IsSensitive && suggestions,
            ["autocapitalizationEnabled"] = !IsSensitive && input.AutoCapitalization,
            ["maliit-inputmethod-hints"] = hints,
            ["enterKeyType"] = input.ReturnKeyType switch
            {
                TextInputReturnKeyType.Return => 1,
                TextInputReturnKeyType.Done => 2,
                TextInputReturnKeyType.Go => 3,
                TextInputReturnKeyType.Send => 4,
                TextInputReturnKeyType.Search => 5,
                TextInputReturnKeyType.Next => 6,
                TextInputReturnKeyType.Previous => 7,
                _ => input.ContentType == TextInputContentType.Search ? 5 : input.Multiline ? 1 : 0
            },
            ["hasSelection"] = !IsSensitive && Cursor != Anchor,
            ["winId"] = WindowId,
            ["cursorRectangle"] = new Struct<int, int, int, int>(CursorRectangle.X, CursorRectangle.Y, CursorRectangle.Width, CursorRectangle.Height),
            ["toolbarId"] = 0
        };
        // Omit, rather than mask, surrounding text for passwords. Never expose secret length/offsets.
        if (!IsSensitive)
        {
            properties["surroundingText"] = SurroundingText;
            properties["cursorPosition"] = Math.Clamp(Cursor, 0, SurroundingText.Length);
            properties["anchorPosition"] = Math.Clamp(Anchor, 0, SurroundingText.Length);
        }
        return properties;
    }
}

/// <summary>Wire callbacks. Text payloads must never be written to diagnostics.</summary>
internal abstract record MaliitEvent;
internal sealed record CommitText(string Text, int ReplacementStart, int ReplacementLength, int CursorPosition) : MaliitEvent;
internal sealed record PreeditText(string Text, int ReplacementStart, int ReplacementLength, int CursorPosition, int FormatCount) : MaliitEvent;
internal sealed record InputKey(int Type, int Key, int Modifiers, string Text, bool Repeat, int Count, byte RequestType) : MaliitEvent;
internal sealed record InputSelection(int Start, int Length) : MaliitEvent;
internal sealed record InputAreaChanged(InputArea Area) : MaliitEvent;
internal sealed record InputHidden : MaliitEvent;
internal sealed record InputActivationLost : MaliitEvent;
internal sealed record InputDisconnected : MaliitEvent;
internal sealed record InputSelectionQuery : MaliitEvent;

/// <summary>One connection belongs to one focus lease; disposing it invalidates all its callbacks.</summary>
internal interface IMaliitSession : IDisposable
{
    /// <summary>Apply the callback before acknowledging it. Must not await another peer call; returns current safe state.</summary>
    Func<MaliitEvent, Task<MaliitWidgetState?>>? DispatchAsync { get; set; }
    bool IsConnected { get; }
    Task OpenAsync(MaliitWidgetState state, CancellationToken cancellationToken);
    Task UpdateAsync(MaliitWidgetState state, CancellationToken cancellationToken);
    Task ShowAsync(CancellationToken cancellationToken);
    Task HideAsync(CancellationToken cancellationToken);
    Task ResetAsync(CancellationToken cancellationToken);
}
