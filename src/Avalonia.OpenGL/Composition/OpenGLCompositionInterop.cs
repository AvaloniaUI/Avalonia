using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Composition;

public static class OpenGLCompositionInterop
{
    /// <summary>
    /// Attempts to create an OpenGL context that is usable with the provided compositor
    /// </summary>
    public static async ValueTask<ICompositionGlContext?> TryCreateCompatibleGlContextAsync(this Compositor compositor,
        OpenGLCompositionInteropContextCreationOptions? options = null)
    {
        compositor.Dispatcher.VerifyAccess();
        options ??= new();
        var gpuInteropTask = compositor.TryGetCompositionGpuInterop();

        var contextSharing =
            (IOpenGlTextureSharingRenderInterfaceContextFeature?)
            await compositor.TryGetRenderInterfaceFeature(
                typeof(IOpenGlTextureSharingRenderInterfaceContextFeature));
        var interop = await gpuInteropTask;
        
        if (interop == null)
            return null;
        
        if (contextSharing != null)
        {
            // If context sharing is advertised, we should always go for it
            var context = contextSharing.CreateSharedContext(options.VersionPreferences);
            if (context == null)
                return null;
            return new CompositionGlContextViaContextSharing(compositor, context, interop, contextSharing);
        }

        if (interop.DeviceLuid == null && interop.DeviceUuid == null)
            return null;
        
        if (AvaloniaLocator.Current.GetService<IPlatformGraphicsOpenGlContextFactory>() is {} factory)
        {
            IGlContext context;
            try
            {
                context = factory.CreateContext(options.VersionPreferences);
            }
            catch
            {
                return null;
            }

            bool success = false;
            try
            {
                var externalObjects = context.TryGetFeature<IGlContextExternalObjectsFeature>();
                if (externalObjects == null)
                    return null;
                
                var luidMatch = interop.DeviceLuid != null 
                                && externalObjects.DeviceLuid != null &&
                                interop.DeviceLuid.SequenceEqual(externalObjects.DeviceLuid);
                var uuidMatch = interop.DeviceLuid != null 
                                && externalObjects.DeviceLuid != null &&
                                interop.DeviceLuid.SequenceEqual(externalObjects.DeviceLuid);

                if (!uuidMatch && !luidMatch)
                    return null;

                foreach (var imageType in externalObjects.SupportedExportableExternalImageTypes)
                {
                    if(!interop.SupportedImageHandleTypes.Contains(imageType))
                        continue;

                    var clientCaps = externalObjects.GetSynchronizationCapabilities(imageType);
                    var serverCaps = interop.GetSynchronizationCapabilities(imageType);
                    var matchingCaps = clientCaps & serverCaps;
                    
                    var syncMode =
                        matchingCaps.HasFlag(CompositionGpuImportedImageSynchronizationCapabilities.Automatic)
                            ? CompositionGpuImportedImageSynchronizationCapabilities.Automatic
                            : matchingCaps.HasFlag(CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex)
                                ? CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex
                                : matchingCaps.HasFlag(CompositionGpuImportedImageSynchronizationCapabilities
                                    .Semaphores)
                                    ? CompositionGpuImportedImageSynchronizationCapabilities.Semaphores
                                    : default;
                    if (syncMode == default)
                        continue;

                    if (syncMode == CompositionGpuImportedImageSynchronizationCapabilities.Semaphores)
                    {
                        var semaphoreType = externalObjects.SupportedExportableExternalSemaphoreTypes
                            .FirstOrDefault(interop.SupportedSemaphoreTypes.Contains);
                        if(semaphoreType == null)
                            continue;
                        success = true;
                        return new CompositionGlContextViaExternalObjects(compositor, context, interop,
                            externalObjects, imageType, syncMode, semaphoreType);
                    }
                    success = true;
                    return new CompositionGlContextViaExternalObjects(compositor, context, interop,
                        externalObjects,
                        imageType, syncMode, null);
                }
            }
            finally
            {
                if(!success)
                    context.Dispose();
            }
        }
        return null;
    }
}

public class OpenGLCompositionInteropContextCreationOptions
{
    public List<GlVersion>? VersionPreferences { get; set; }
}