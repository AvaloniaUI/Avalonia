#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace Avalonia.UnitTests;

/// <summary>
/// Runs tests in the invariant culture.
/// </summary>
/// <remarks>
/// Some tests check exception messages, and those from the .NET framework will be translated.
/// Some tests are formatting numbers, expecting a dot as a decimal point.
/// Use this fixture to set the current culture to the invariant culture.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class InvariantCultureAttribute : BeforeAfterTestAttribute
{
    private CultureInfo? _previousCulture;
    private CultureInfo? _previousUICulture;

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        base.Before(methodUnderTest, test);

        _previousCulture = CultureInfo.CurrentCulture;
        _previousUICulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _previousCulture!;
        CultureInfo.CurrentUICulture = _previousUICulture!;

        base.After(methodUnderTest, test);
    }
}
