using System;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

internal class MaliitInputMethodManager
{
    // A terminal hide must finish before another window activates its context.
    internal Task PendingDismissal { get; set; } = Task.CompletedTask;
    internal InputArea LastArea { get; set; }
    internal Func<IMaliitSession> CreateSession { get; }

    internal MaliitInputMethodManager(Func<IMaliitSession>? createSession = null)
        => CreateSession = createSession ?? (() => new MaliitSession());
}
