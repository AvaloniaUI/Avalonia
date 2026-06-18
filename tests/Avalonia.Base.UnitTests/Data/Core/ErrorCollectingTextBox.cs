using System;
using Avalonia.Controls;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

/// <summary>
/// A <see cref="TextBox"/> which stores the latest binding error state.
/// </summary>
public class ErrorCollectingTextBox : TextBox
{
    public Exception? Error { get; private set; }
    public BindingValueType ErrorState { get; private set; }

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        if (property == TextProperty)
        {
            Error = error;
            ErrorState = state;
        }

        base.UpdateDataValidation(property, state, error);
    }
}
