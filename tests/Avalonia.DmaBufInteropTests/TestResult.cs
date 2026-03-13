namespace Avalonia.DmaBufInteropTests;

public enum TestStatus
{
    Passed,
    Failed,
    Skipped
}

public record TestResult(string Name, TestStatus Status, string? Message = null)
{
    public override string ToString()
    {
        var tag = Status switch
        {
            TestStatus.Passed => "PASS",
            TestStatus.Failed => "FAIL",
            TestStatus.Skipped => "SKIP",
            _ => "????"
        };
        var suffix = Message != null ? $" — {Message}" : "";
        return $"[{tag}] {Name}{suffix}";
    }
}
