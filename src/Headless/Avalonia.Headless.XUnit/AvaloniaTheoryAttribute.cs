using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Identifies an xunit theory that starts on Avalonia Dispatcher
/// such that awaited expressions resume on the test's "main thread".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Avalonia.Headless.XUnit.AvaloniaTheoryDiscoverer", "Avalonia.Headless.XUnit")]
public sealed class AvaloniaTheoryAttribute : TheoryAttribute
{
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class AvaloniaTheoryDiscoverer : TheoryDiscoverer
{
    public AvaloniaTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink)
    {
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
    {
        yield return new AvaloniaTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataRow);
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
    {
        yield return new AvaloniaTheoryTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), TestMethodDisplayOptions.None, testMethod);
    }
}
