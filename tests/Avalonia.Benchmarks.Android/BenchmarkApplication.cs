using global::Android.App;
using global::Android.Runtime;

namespace Avalonia.Benchmarks.Android;

[Application]
public class BenchmarkApplication : global::Android.App.Application
{
    protected BenchmarkApplication(nint handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
    }
}
