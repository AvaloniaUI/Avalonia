using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Interactivity;
using IntegrationTestApp.Embedding;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.ObjCRuntime;

namespace IntegrationTestApp;

public partial class EmbeddingPage : UserControl
{
    private const long NSModalResponseContinue = -1002;

    public EmbeddingPage()
    {
        InitializeComponent();
        ResetText();
    }

    private void ResetText()
    {
        NativeTextBox.Text = NativeTextBoxInPopup.Text = "Native text box";
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        ResetText();
        ModalResultTextBox.Text = "";
    }

    private void RunNativeModalSession_OnClick(object? sender, RoutedEventArgs e)
    {
        MacHelper.EnsureInitialized();

        var app = NSApplication.SharedApplication;
        var modalWindow = CreateNativeWindow();
        var session = app.BeginModalSession(modalWindow);

        while (true)
        {
            if (app.RunModalSession(session) != NSModalResponseContinue)
                break;
        }

        app.EndModalSession(session);
    }

    private NSWindow CreateNativeWindow()
    {
        var button = new Button
        {
            Name = "ButtonInModal",
            Content = "Button"
        };

        AutomationProperties.SetAutomationId(button, "ButtonInModal");

        var root = new EmbeddableControlRoot
        {
            Width = 200,
            Height = 200,
            Content = button
        };
        root.Prepare();

        var window = new NSWindow(
            new CGRect(0, 0, root.Width, root.Height),
            NSWindowStyle.Titled | NSWindowStyle.Closable,
            NSBackingStore.Buffered,
            false);

        window.Identifier = "ModalNativeWindow";
        window.WillClose += (_, _) => NSApplication.SharedApplication.StopModal();

        button.Click += (_, _) =>
        {
            ModalResultTextBox.Text = "Clicked";
            window.Close();
        };

        if (root.TryGetPlatformHandle() is not { } handle)
            throw new InvalidOperationException("Could not get platform handle");

        window.ContentView = (NSView)Runtime.GetNSObject(handle.Handle)!;
        root.StartRendering();

        return window;
    }
}
