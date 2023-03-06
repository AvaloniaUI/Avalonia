using System;
using System.Diagnostics;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public class SplitTimer
{
    private DateTime _split;

    public SplitTimer()
    {
        Reset();
    }
    
    public void Reset()
    {
        _split = DateTime.Now;
    }

    public TimeSpan Split()
    {
        var now = DateTime.Now;
        var result = now - _split;
        
        _split = now;

        return result;
    }

    public void SplitLog(string section)
    {
        Debug.WriteLine($"{section} took: {Split().TotalMilliseconds} ms");
    }
}
