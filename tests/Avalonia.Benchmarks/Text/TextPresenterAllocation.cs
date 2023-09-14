using System;
using System.Threading.Tasks;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Text;

[MemoryDiagnoser]
public class TextPresenterAllocation : IDisposable
{
    private readonly IDisposable _app;
    private readonly Controls.Presenters.TextPresenter _presenter;

    public TextPresenterAllocation()
    {
        _app = UnitTestApplication.Start(TestServices.StyledWindow);

        var root = new TestRoot(true, null)
        {
            Renderer = new NullRenderer()
        };

        _presenter = new()
        {
            Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&?%$@",
        };

        root.Child = _presenter;

        root.LayoutManager.ExecuteInitialLayoutPass();
    }

    [Benchmark]
    public async Task CarretBlink()
    {
        _presenter.ShowCaret();
        await Task.Delay(10000);
    }

    public void Dispose()
    {
        _app.Dispose();
    }
}
