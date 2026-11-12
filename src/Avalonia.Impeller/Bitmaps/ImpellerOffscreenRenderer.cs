using System;
using System.Collections.Concurrent;
using System.Threading;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;

namespace Avalonia.Impeller;

/// <summary>
/// Renders on a private OpenGL context owned by a worker thread, for pixels needed outside the compositor.
/// Other backends can't read pixels back, so callers fall back to Skia.
/// </summary>
internal static class ImpellerOffscreenRenderer
{
    private static readonly object s_lock = new();
    private static Worker? s_worker;
    private static bool s_unsupported;

    /// <summary>Runs <paramref name="job"/> on the offscreen context, or returns default if there is none.</summary>
    public static T? TryRun<T>(Func<ImpellerBackendContext, T> job) where T : class
    {
        Worker worker;
        lock (s_lock)
        {
            if (s_unsupported)
                return null;
            if (s_worker is null || s_worker.IsLost || s_worker.Failed)
            {
                s_worker?.Dispose();
                s_worker = new Worker();
                if (s_worker.Unsupported)
                {
                    s_unsupported = true;
                    s_worker.Dispose();
                    s_worker = null;
                    return null;
                }
            }
            worker = s_worker;
        }
        return worker.Failed ? null : worker.Invoke(job);
    }

    private sealed class Worker : IDisposable
    {
        private readonly BlockingCollection<Action> _jobs = new();
        private IPlatformGraphicsContext? _graphics;
        private ImpellerBackendContext? _context;

        public Worker()
        {
            using var ready = new ManualResetEventSlim();
            var thread = new Thread(() => Run(ready)) { IsBackground = true, Name = "Impeller offscreen" };
            thread.Start();
            ready.Wait();
        }

        public bool IsLost => _graphics?.IsLost == true;

        // Creation is retried: the platform may set up its graphics later in startup.
        public bool Failed => _context is null;

        public bool Unsupported { get; private set; }

        private void Run(ManualResetEventSlim ready)
        {
            try
            {
                _graphics = CreateGraphicsContext();
                if (_graphics is IGlContext gl)
                {
                    using (_graphics.EnsureCurrent())
                        _context = ImpellerGlBackendContext.Create(gl);
                }
                else
                {
                    Unsupported = _graphics is not null;
                }
            }
            catch (Exception e)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, Logging.LogArea.Visual)
                    ?.Log(null, "Impeller can't create an offscreen context: {Exception}", e);
            }
            finally
            {
                ready.Set();
            }

            foreach (var job in _jobs.GetConsumingEnumerable())
                job();

            _context?.Dispose();
            _graphics?.Dispose();
        }

        private static IPlatformGraphicsContext? CreateGraphicsContext()
        {
            if (AvaloniaLocator.Current.GetService<IPlatformGraphics>() is { } platform)
            {
                try
                {
                    return platform.CreateContext();
                }
                catch (NotSupportedException) when (s_surfaceless.Value is not null)
                {
                }
            }

            // Bitmaps can be rendered before the platform has set up its graphics, or without a display.
            return s_surfaceless.Value?.CreateContext();
        }

        private static readonly Lazy<IPlatformGraphics?> s_surfaceless = new(() =>
            OperatingSystem.IsLinux()
                ? EglPlatformGraphics.TryCreate(() => new EglDisplay(new EglDisplayCreationOptions
                {
                    Egl = new EglInterface(),
                    PlatformType = EglConsts.EGL_PLATFORM_SURFACELESS_MESA,
                    SupportsMultipleContexts = true,
                    SupportsContextSharing = true,
                    AllowPbufferOnlyConfigs = true,
                    GlVersions = [new GlVersion(GlProfileType.OpenGLES, 3, 0)],
                }))
                : null);

        public T Invoke<T>(Func<ImpellerBackendContext, T> job)
        {
            T result = default!;
            Exception? error = null;
            using var done = new ManualResetEventSlim();
            _jobs.Add(() =>
            {
                try
                {
                    using (_graphics!.EnsureCurrent())
                    {
                        result = job(_context!);
                        // Nothing presents here.
                        _context!.EndFrame();
                        _context.RetireFrameResources();
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    done.Set();
                }
            });
            done.Wait();
            if (error is not null)
                throw new InvalidOperationException("Offscreen rendering failed.", error);
            return result;
        }

        public void Dispose() => _jobs.CompleteAdding();
    }
}
