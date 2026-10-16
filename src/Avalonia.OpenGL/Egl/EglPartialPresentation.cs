using System;
using Avalonia.Platform;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.OpenGL.Egl;

// One state per window render target. Rectangle arrays are reused between sessions.
internal sealed class EglPartialPresentation
{
    private readonly IEglInterface _egl;
    private readonly IntPtr _display;
    private readonly bool _partialUpdate;
    private readonly bool _swapKhr;
    private readonly bool _swapExt;
    private readonly Action _invalidateTarget;
    private readonly EglBufferAgeTracker _buffers = new();
    private IntPtr _surface;
    private PixelSize _size;
    private bool _canSetDirtyRects;
    private bool _isYFlipped;
    private int _consecutiveZeroAgeFrames;
    private int[] _frameDirtyRects = Array.Empty<int>();
    private int[] _bufferDirtyRects = Array.Empty<int>();

    public EglPartialPresentation(IEglInterface egl, IntPtr display, bool partialUpdate,
        bool swapKhr, bool swapExt, Action invalidateTarget)
    {
        _egl = egl;
        _display = display;
        _partialUpdate = partialUpdate;
        _swapKhr = swapKhr;
        _swapExt = swapExt;
        _invalidateTarget = invalidateTarget;
    }

    public bool PreferRetainedLayer { get; private set; }

    public Session BeginDraw(IntPtr surface, PixelSize size, bool isYFlipped = false)
    {
        if (_surface != surface || _size != size || _isYFlipped != isYFlipped)
        {
            Invalidate();
            _surface = surface;
            _size = size;
            _isYFlipped = isYFlipped;
            if (_partialUpdate)
            {
                if (_egl.QuerySurface(_display, surface, EGL_SWAP_BEHAVIOR, out var behavior) == 0)
                    throw OpenGlException.GetFormattedEglException("eglQuerySurface", _egl.GetError());
                _canSetDirtyRects = behavior == EGL_BUFFER_DESTROYED;
            }
        }

        if (_egl.QuerySurface(_display, surface, EGL_BUFFER_AGE_EXT, out var age) == 0)
            throw OpenGlException.GetFormattedEglException("eglQuerySurface(EGL_BUFFER_AGE_EXT)", _egl.GetError());
        return new Session(this, _buffers.Acquire(age), age);
    }

    public void Invalidate()
    {
        _buffers.Invalidate();
        _surface = IntPtr.Zero;
        _size = default;
        _consecutiveZeroAgeFrames = 0;
    }

    internal static int WriteRects(ReadOnlySpan<PixelRect> rects, PixelSize size, ref int[] destination,
        bool isYFlipped = false)
    {
        var required = Math.Max(4, rects.Length * 4);
        if (destination.Length < required)
            Array.Resize(ref destination, required);
        var count = 0;
        foreach (var rect in rects)
        {
            var clipped = rect.Intersect(new PixelRect(size));
            if (clipped.Width <= 0 || clipped.Height <= 0)
                continue;
            destination[count++] = clipped.X;
            destination[count++] = isYFlipped ? clipped.Y : size.Height - clipped.Bottom;
            destination[count++] = clipped.Width;
            destination[count++] = clipped.Height;
        }
        // Both EGL extensions interpret n_rects == 0 as a full-surface update. A single
        // zero-area rectangle represents an empty region, including after clamping.
        if (count == 0)
        {
            Array.Clear(destination, 0, 4);
            return 1;
        }
        return count / 4;
    }

    internal sealed class Session : IPartialPresentationFeature, IDisposable
    {
        private readonly EglPartialPresentation _owner;
        private readonly EglBufferAgeTracker.Buffer _buffer;
        private readonly int _age;
        private int _frameDirtyRectCount;
        private bool _dirtyRectsSet;
        private bool _nativeDirtyRectsDeclared;
        private bool _succeeded;
        private bool _finished;

        public Session(EglPartialPresentation owner, EglBufferAgeTracker.Buffer buffer, int age)
        {
            _owner = owner;
            _buffer = buffer;
            _age = age;
        }

        public IPartialPresentationBuffer Buffer => _buffer;
        public bool CanPresent => _succeeded && !_finished;

        public void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects)
        {
            ObjectDisposedException.ThrowIf(_finished, this);
            if (_dirtyRectsSet)
                throw new InvalidOperationException("Dirty rectangles can only be declared once per rendering session.");
            _dirtyRectsSet = true;
            _frameDirtyRectCount = WriteRects(frameDirtyRects, _owner._size, ref _owner._frameDirtyRects, _owner._isYFlipped);
            if (_owner._canSetDirtyRects)
            {
                var count = WriteRects(bufferDirtyRects, _owner._size, ref _owner._bufferDirtyRects, _owner._isYFlipped);
                _nativeDirtyRectsDeclared = true;
                if (_owner._egl.SetDamageRegionKhr(_owner._display, _owner._surface, _owner._bufferDirtyRects, count) == 0)
                {
                    var error = _owner._egl.GetError();
                    Dispose();
                    throw OpenGlException.GetFormattedEglException("eglSetDamageRegionKHR", error);
                }
            }
        }

        public void Succeeded()
        {
            ObjectDisposedException.ThrowIf(_finished, this);
            _succeeded = true;
        }

        public void Dispose()
        {
            if (_finished)
                return;
            _finished = true;
            _buffer.IsValid = false;
            _owner.Invalidate();
            // Only a native dirty rectangle declaration requires a frame boundary before retrying.
            if (_nativeDirtyRectsDeclared)
                _owner._invalidateTarget();
        }

        public bool Present()
        {
            ObjectDisposedException.ThrowIf(_finished, this);
            if (!_succeeded)
                throw new InvalidOperationException("Rendering must succeed before presentation.");
            var egl = _owner._egl;
            uint success;
            if (_dirtyRectsSet && _owner._swapKhr)
                success = egl.SwapBuffersWithDamageKhr(_owner._display, _owner._surface,
                    _owner._frameDirtyRects, _frameDirtyRectCount);
            else if (_dirtyRectsSet && _owner._swapExt)
                success = egl.SwapBuffersWithDamageExt(_owner._display, _owner._surface,
                    _owner._frameDirtyRects, _frameDirtyRectCount);
            else
                success = egl.SwapBuffersChecked(_owner._display, _owner._surface);
            if (success == 0)
            {
                var error = egl.GetError();
                Dispose();
                if (!_nativeDirtyRectsDeclared)
                    _owner._invalidateTarget();
                // Window teardown is local to this target. Let other windows render
                // this tick; the compositor will recreate this target on its next draw.
                if (error == EGL_BAD_SURFACE || error == EGL_BAD_NATIVE_WINDOW)
                    return false;
                throw OpenGlException.GetFormattedEglException("eglSwapBuffers", error);
            }
            _owner._buffers.Presented(_buffer);
            _owner._consecutiveZeroAgeFrames = _age == 0 ? Math.Min(8, _owner._consecutiveZeroAgeFrames + 1) : 0;
            // Keep partial output copies, but avoid full scene redraws on drivers
            // that never provide reusable buffers. Keep this preference for the target's lifetime.
            if (_owner._consecutiveZeroAgeFrames >= 8)
                _owner.PreferRetainedLayer = true;
            _finished = true;
            return true;
        }
    }
}
