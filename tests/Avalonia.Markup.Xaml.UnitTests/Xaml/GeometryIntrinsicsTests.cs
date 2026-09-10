using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class GeometryIntrinsicsTests : XamlTestBase
{
    [Fact]
    public void Element_Syntax_Geometry_Is_Compiled()
    {
        // We don't verify IL output to be sure it's geometry is compile-time parsed.
        using var _ = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var path = AvaloniaRuntimeXamlLoader.Parse<Path>(
            """
            <Path xmlns='https://github.com/avaloniaui'>
                <Path.Data>
                    <StreamGeometry>M 0 0 L 10 10 Z</StreamGeometry>
                </Path.Data>
            </Path>
            """);

        Assert.IsType<StreamGeometry>(path.Data);
    }

    [Fact]
    public void Invalid_Geometry_Reports_Diagnostic()
    {
        var diagnostics = new List<RuntimeXamlDiagnostic>();

        var xamlDocument = new RuntimeXamlLoaderDocument(
            """
            <Path xmlns='https://github.com/avaloniaui'
                  Data='M 0 0 X 1 1' />
            """);

        Assert.ThrowsAny<Exception>(() => AvaloniaRuntimeXamlLoader.Load(
            xamlDocument,
            new RuntimeXamlLoaderConfiguration
            {
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            }));

        Assert.Contains(diagnostics, d =>
            d is { Severity: RuntimeXamlDiagnosticSeverity.Error, Title: "Unable to parse \"M 0 0 X 1 1\" as a geometry" });
    }
}
