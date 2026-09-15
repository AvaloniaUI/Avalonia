using System;
using System.Collections.Generic;
using Avalonia.OpenGL.Features;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Egl;

internal partial class EglExternalObjectsFeature : IGlContextExternalObjectsFeature
{
    private readonly EglContext _context;
    private readonly ExternalObjectsOpenGlExtensionFeature? _glExternalObjects;
    private readonly List<string> _imageTypes = new();

    public static EglExternalObjectsFeature? TryCreate(EglContext context)
    {
        var feature = new EglExternalObjectsFeature(context);
        return feature._imageTypes.Count > 0 || feature._glExternalObjects != null ? feature : null;
    }

    private EglExternalObjectsFeature(EglContext context)
    {
        _context = context;
        _glExternalObjects = ExternalObjectsOpenGlExtensionFeature.TryCreate(context);
        if (_glExternalObjects != null)
            _imageTypes.AddRange(_glExternalObjects.SupportedImportableExternalImageTypes);

        if (TryInitializeDrm())
            _imageTypes.Add(KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor);
    }

    private static bool IsDmaBuf(string? type) =>
        type == KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor;

    public IReadOnlyList<string> SupportedImportableExternalImageTypes => _imageTypes;

    public IReadOnlyList<string> SupportedExportableExternalImageTypes =>
        _glExternalObjects?.SupportedExportableExternalImageTypes ?? Array.Empty<string>();

    public IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes =>
        _glExternalObjects?.SupportedImportableExternalSemaphoreTypes ?? Array.Empty<string>();

    public IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes =>
        _glExternalObjects?.SupportedExportableExternalSemaphoreTypes ?? Array.Empty<string>();

    public IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type)
    {
        if (IsDmaBuf(type))
            return GetSupportedDmaBufImageFormats();
        return _glExternalObjects?.GetSupportedFormatsForExternalMemoryType(type)
               ?? Array.Empty<PlatformGraphicsExternalImageFormat>();
    }

    public IGlExportableExternalImageTexture CreateImage(string type, PixelSize size,
        PlatformGraphicsExternalImageFormat format)
    {
        if (IsDmaBuf(type) || _glExternalObjects == null)
            throw new NotSupportedException();
        return _glExternalObjects.CreateImage(type, size, format);
    }

    public IGlExportableExternalImageTexture CreateSemaphore(string type)
    {
        if (_glExternalObjects == null)
            throw new NotSupportedException();
        return _glExternalObjects.CreateSemaphore(type);
    }

    public IGlExternalImageTexture ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
    {
        if (IsDmaBuf(handle.HandleDescriptor))
            return ImportDmaBufImage(handle, properties);
        if (_glExternalObjects == null)
            throw new ArgumentException(handle.HandleDescriptor + " is not supported", nameof(handle));
        return _glExternalObjects.ImportImage(handle, properties);
    }

    public IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        if (_glExternalObjects == null)
            throw new ArgumentException(handle.HandleDescriptor + " is not supported", nameof(handle));
        return _glExternalObjects.ImportSemaphore(handle);
    }

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        if (IsDmaBuf(imageHandleType))
            return CompositionGpuImportedImageSynchronizationCapabilities.Automatic;
        return _glExternalObjects?.GetSynchronizationCapabilities(imageHandleType) ?? default;
    }

    public byte[]? DeviceLuid => _glExternalObjects?.DeviceLuid;
    public byte[]? DeviceUuid => _glExternalObjects?.DeviceUuid;
}
