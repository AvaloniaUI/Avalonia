using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

/// <summary>
/// Provides OpenGL interop facilities for the composition engine.
/// </summary>
public static class OpenGlCompositionInterop
{
    /// <summary>
    /// Attempts to create an OpenGL context that is suitable for interop with the compositor's GPU context.
    /// Must be called on the thread the compositor belongs to.
    /// </summary>
    /// <returns>
    /// The created context, or null when the current platform or compositor configuration doesn't
    /// support OpenGL interop. The concrete reason is logged to the "OpenGL" log area.
    /// </returns>
    public static async ValueTask<ICompositionGlContext?> TryCreateCompatibleGlContextAsync(
        this Compositor compositor, CompositionGlContextOptions? options = null)
    {
        if (compositor == null)
            throw new ArgumentNullException(nameof(compositor));
        compositor.Dispatcher.VerifyAccess();

        var gpuInteropTask = compositor.TryGetCompositionGpuInterop();

        var sharingFeature =
            (IOpenGlTextureSharingRenderInterfaceContextFeature?)
            await compositor.TryGetRenderInterfaceFeature(
                typeof(IOpenGlTextureSharingRenderInterfaceContextFeature));
        var interop = await gpuInteropTask;

        if (interop == null)
        {
            // Expected on platforms without GPU interop support, so not an error
            LogUnsupported("Compositor backend doesn't support GPU interop");
            return null;
        }

        if (sharingFeature?.CanCreateSharedContext == true)
        {
            IGlContext? context = null;
            try
            {
                context = sharingFeature.CreateSharedContext(options?.GlProfiles);
            }
            catch (Exception e)
            {
                LogError("Unable to create a context shared with the compositor: {exception}", e);
            }

            if (context == null)
                LogError("Unable to create a context shared with the compositor");
            else
                return new CompositionGlContext(compositor, context, interop, sharingFeature, null);
        }

        if (AvaloniaLocator.Current.GetService<IPlatformGraphicsOpenGlContextFactory>() is not { } contextFactory)
        {
            LogUnsupported("The current platform doesn't provide an OpenGL context factory");
            return null;
        }

        IGlContext? ctx = null;
        try
        {
            ctx = contextFactory.CreateContext(options?.GlProfiles);
            if (ctx.TryGetFeature<IGlContextExternalObjectsFeature>(out var externalObjects)
                && interop.SupportedImageHandleTypes.Contains(
                    KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle)
                && externalObjects.SupportedExportableExternalImageTypes.Contains(
                    KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle))
                return new CompositionGlContext(compositor, ctx, interop, null, externalObjects);

            LogUnsupported("The current platform doesn't support OpenGL context sharing with the compositor " +
                "or a compatible shared memory type");
            ctx.Dispose();
            return null;
        }
        catch (Exception e)
        {
            LogError("Unable to create an OpenGL context: {exception}", e);
            ctx?.Dispose();
            return null;
        }
    }

    private static void LogUnsupported(string message) =>
        Logger.TryGet(LogEventLevel.Warning, "OpenGL")?.Log(nameof(OpenGlCompositionInterop), message);

    private static void LogError(string message, params object?[] args) =>
        Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(nameof(OpenGlCompositionInterop), message, args);
}
