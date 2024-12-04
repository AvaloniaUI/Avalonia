using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android;

public interface IAvaloniaActivity : IActivityResultHandler, IActivityNavigationService
{
    object? Content { get; set; }
    event EventHandler<ActivatedEventArgs>? Activated;
    event EventHandler<ActivatedEventArgs>? Deactivated;
}
