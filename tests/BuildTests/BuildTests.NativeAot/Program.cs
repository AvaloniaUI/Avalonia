using System;

namespace BuildTests.NativeAot;

internal static class Program
{
    public static void Main()
    {
        var view = new MainView();
        Console.Out.WriteLine(view.TextBlock.Text);
    }
}
