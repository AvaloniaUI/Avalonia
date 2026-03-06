#nullable enable

using Avalonia.Controls.Utils;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Utils;

public class BindingEvaluatorTests : ScopedTestBase
{
    [Fact]
    public void ClearDataContext_Sets_DataContext_To_Null()
    {
        var evaluator = new BindingEvaluator<string?>();
        evaluator.Evaluate("foo");
        Assert.Equal("foo", evaluator.DataContext);

        evaluator.ClearDataContext();
        Assert.Null(evaluator.DataContext);
    }
}
