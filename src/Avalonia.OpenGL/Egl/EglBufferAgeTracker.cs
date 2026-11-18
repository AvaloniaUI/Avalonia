using System;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Egl;

// EGL exposes the age of buffer contents, rather than a swapchain index.
internal sealed class EglBufferAgeTracker
{
    private readonly Buffer?[] _history = new Buffer[16];
    private int _next;
    private int _count;

    public Buffer Acquire(int age)
    {
        if (age > 0 && age <= _count &&
            _history[(_next - age + _history.Length) % _history.Length] is { IsValid: true } buffer)
            return buffer;
        return new Buffer();
    }

    public void Presented(Buffer buffer)
    {
        var evicted = _history[_next];
        _history[_next] = buffer;
        _next = (_next + 1) % _history.Length;
        _count = Math.Min(_count + 1, _history.Length);
        // A buffer can occur in several history slots. Keep it valid until its last slot expires.
        if (evicted != null && Array.IndexOf(_history, evicted) < 0)
            evicted.IsValid = false;
    }

    public void Invalidate()
    {
        foreach (var buffer in _history)
            if (buffer != null)
                buffer.IsValid = false;
        Array.Clear(_history);
        _next = _count = 0;
    }

    internal sealed class Buffer : IPartialPresentationBuffer
    {
        public bool IsValid { get; internal set; } = true;
        public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);
    }
}
