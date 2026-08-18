using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

[EditorBrowsable(EditorBrowsableState.Never)]
public class AvaloniaTheoryDiscoverer : TheoryDiscoverer
{
    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object?[] testMethodArguments)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(
            discoveryOptions,
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments);
        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);

        var testCase = new AvaloniaTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.Explicit,
            details.SkipExceptions,
            details.SkipReason,
            details.SkipType,
            details.SkipUnless,
            details.SkipWhen,
            traits,
            testMethodArguments,
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            timeout: details.Timeout);

        return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([testCase]);
    }

    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, theoryAttribute);

        var testCase =
            details.SkipReason is not null && details.SkipUnless is null && details.SkipWhen is null
                ? new AvaloniaTestCase(
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
                    timeout: details.Timeout
                )
                : (IXunitTestCase)new AvaloniaDelayEnumeratedTheoryTestCase(
                    details.ResolvedTestMethod,
                    details.TestCaseDisplayName,
                    details.UniqueID,
                    details.Explicit,
                    theoryAttribute.SkipTestWithoutData,
                    details.SkipExceptions,
                    details.SkipReason,
                    details.SkipType,
                    details.SkipUnless,
                    details.SkipWhen,
                    testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase),
                    sourceFilePath: details.SourceFilePath,
                    sourceLineNumber: details.SourceLineNumber,
                    timeout: details.Timeout
                );

        return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([testCase]);
    }
}
