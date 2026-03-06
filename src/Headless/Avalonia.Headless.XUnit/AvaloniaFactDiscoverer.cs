using System;
using System.ComponentModel;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

[EditorBrowsable(EditorBrowsableState.Never)]
public class AvaloniaFactDiscoverer : FactDiscoverer
{
    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

        return new AvaloniaTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.Explicit,
            details.SkipExceptions,
            details.SkipReason,
            details.SkipType,
            details.SkipUnless,
            details.SkipWhen,
            testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            timeout: details.Timeout);
    }
}
