#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace Avalonia.UnitTests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UseEmptyDesignatorCultureAttribute : BeforeAfterTestAttribute
{
    private CultureInfo? _previousCulture;
    private CultureInfo? _previousUICulture;

    private CultureInfo CultureInfo { get; } =
        new(string.Empty, false) { DateTimeFormat = { AMDesignator = string.Empty, PMDesignator = string.Empty } };

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        base.Before(methodUnderTest, test);

        _previousCulture = CultureInfo.CurrentCulture;
        _previousUICulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = CultureInfo;
        CultureInfo.CurrentUICulture = CultureInfo;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _previousCulture!;
        CultureInfo.CurrentUICulture = _previousUICulture!;

        base.After(methodUnderTest, test);
    }
}
