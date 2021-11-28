using System;

namespace Avalonia.Blazor
{
    public static class AvaloniaBlazor
    {
        public static IDisposable Lock() => BlazorWindowingPlatform.Lock();
    }
}