using Xunit;

namespace Avalonia.Controls.UnitTests;

public class ProgressBarTests
{
    [Fact]
    public void Indeterminate_Animation_Is_Not_Running_When_IsVisible_false()
    {
        var progressBar = new ProgressBar()
        {
            IsIndeterminate = true,
        };

        Assert.Contains(":indeterminate", progressBar.Classes);

        progressBar.IsVisible = false;

        Assert.DoesNotContain(":indeterminate", progressBar.Classes);
    }
}
