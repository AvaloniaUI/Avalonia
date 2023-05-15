using System;
using System.ComponentModel;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Identifies an xunit test that starts on Avalonia Dispatcher
/// such that awaited expressions resume on the test's "main thread".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Avalonia.Headless.XUnit.AvaloniaUIFactDiscoverer", "Avalonia.Headless.XUnit")]
public sealed class AvaloniaFactAttribute : FactAttribute
{
    
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class AvaloniaUIFactDiscoverer : FactDiscoverer
{
    private readonly IMessageSink diagnosticMessageSink;
    public AvaloniaUIFactDiscoverer(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink)
    {
        this.diagnosticMessageSink = diagnosticMessageSink;
    }

    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return new AvaloniaTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
    }
}
