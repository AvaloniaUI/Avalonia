using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.Versioning;
using Avalonia.Browser;

namespace Avalonia;

internal class BrowserSingleViewLifetime : ISingleViewApplicationLifetime
{
    public AvaloniaView? View;

    public Control? MainView
    {
        get
        {
            EnsureView();
            return View.Content;
        }
        set
        {
            EnsureView();
            View.Content = value;
        }
    }

    [MemberNotNull(nameof(View))]
    private void EnsureView()
    {
        if (View is null)
        {
            throw new InvalidOperationException("Browser lifetime was not initialized. Make sure AppBuilder.StartBrowserApp was called.");
        }
    }
}
