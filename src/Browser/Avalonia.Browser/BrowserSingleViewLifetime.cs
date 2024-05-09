using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia;

internal class BrowserSingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
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
            throw new InvalidOperationException(
                "Browser lifetime was not initialized. Make sure AppBuilder.StartBrowserAppAsync was called.");
        }
    }

    public TopLevel? TopLevel => View?.TopLevel;
}
