using System.Runtime.CompilerServices;

namespace BuildTests;

public sealed class MainViewModel
{
    public string HelloText { get; set; } = $"Hello from {(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "AOT")}";
}
