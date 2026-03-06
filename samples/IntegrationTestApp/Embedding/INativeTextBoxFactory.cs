using System;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal interface INativeTextBoxImpl
{
    IPlatformHandle Handle { get; }
    string Text { get; set; }
    event EventHandler? ContextMenuRequested;
    event EventHandler? Hovered;
    event EventHandler? PointerExited;
}

internal interface INativeTextBoxFactory
{
    INativeTextBoxImpl CreateControl(IPlatformHandle parent);
}
