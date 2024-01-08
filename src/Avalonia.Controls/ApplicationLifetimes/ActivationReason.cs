namespace Avalonia.Controls.ApplicationLifetimes;

public enum ActivationReason
{
    Launched = 10,
    OpenUri = 20,
    Reopen = 30, // i.e. Dock Icon clicked on MacOS
    Background = 40 // i.e. Entered or left background.
}
