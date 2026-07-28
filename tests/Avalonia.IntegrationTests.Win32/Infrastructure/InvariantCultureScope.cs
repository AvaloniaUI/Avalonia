using System;
using System.Globalization;

namespace Avalonia.IntegrationTests.Win32.Infrastructure;

internal sealed class InvariantCultureScope : IDisposable
{
    private readonly CultureInfo? _previousCulture;
    private readonly CultureInfo? _previousUICulture;

    public InvariantCultureScope()
    {
        _previousCulture = CultureInfo.CurrentCulture;
        _previousUICulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        if (_previousCulture is not null)
            CultureInfo.CurrentCulture = _previousCulture;

        if (_previousUICulture is not null)
            CultureInfo.CurrentUICulture = _previousUICulture;
    }
}

