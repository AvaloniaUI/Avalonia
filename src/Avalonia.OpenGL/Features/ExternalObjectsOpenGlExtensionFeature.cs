using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.SourceGenerator;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL.Features;

unsafe partial class ExternalObjectsInterface
{
    public ExternalObjectsInterface(Func<string, IntPtr> getProcAddress)
    {
        Initialize(getProcAddress);
    }
    
    [GetProcAddress("glImportMemoryFdEXT", true)]
    public partial void ImportMemoryFdEXT(uint memory, ulong size, int handleType, int fd);
    
    [GetProcAddress("glImportSemaphoreFdEXT", true)]
    public partial void ImportSemaphoreFdEXT(uint semaphore,
                              int handleType,
                              int fd);
    
    [GetProcAddress("glCreateMemoryObjectsEXT")]
    public partial void CreateMemoryObjectsEXT(int n, out uint memoryObjects);
    
    [GetProcAddress("glDeleteMemoryObjectsEXT")]
    public partial void DeleteMemoryObjectsEXT(int n, ref uint objects);

    [GetProcAddress("glTexStorageMem2DEXT")]
    public partial void TexStorageMem2DEXT(int target, int levels, int internalFormat, int width, int height,
        uint memory, ulong offset);
    
    [GetProcAddress("glGenSemaphoresEXT")]
    public partial void GenSemaphoresEXT(int n, out uint semaphores);

    [GetProcAddress("glDeleteSemaphoresEXT")]
    public partial void DeleteSemaphoresEXT(int n, ref uint semaphores);
    
    [GetProcAddress("glWaitSemaphoreEXT")]
    public partial void WaitSemaphoreEXT(uint semaphore,
        uint numBufferBarriers, uint* buffers,
        uint numTextureBarriers, int* textures,
        int* srcLayouts);
    
    [GetProcAddress("glSignalSemaphoreEXT")]
    public partial void SignalSemaphoreEXT(uint semaphore,
        uint numBufferBarriers, uint* buffers,
        uint numTextureBarriers, int* textures,
        int* dstLayouts);
    
    
    [GetProcAddress("glGetUnsignedBytei_vEXT", true)]
    public partial void GetUnsignedBytei_vEXT(int target, uint index, byte* data);
    
    [GetProcAddress("glGetUnsignedBytevEXT", true)]
    public partial void GetUnsignedBytevEXT(int target, byte* data);
}

public class ExternalObjectsOpenGlExtensionFeature : IGlContextExternalObjectsFeature
{
    private readonly IGlContext _context;
    private readonly ExternalObjectsInterface _ext;
    private readonly List<string> _imageTypes = new();
    private readonly List<string> _semaphoreTypes = new();

    public static ExternalObjectsOpenGlExtensionFeature? TryCreate(IGlContext context)
    {
        var extensions = context.GlInterface.GetExtensions();
        if (extensions.Contains("GL_EXT_memory_object") && extensions.Contains("GL_EXT_semaphore"))
        {
            try
            {
                return new ExternalObjectsOpenGlExtensionFeature(context, extensions);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(nameof(ExternalObjectsOpenGlExtensionFeature),
                    "Unable to initialize EXT_external_objects extension: " + e);
            }
        }

        return null;
    }

    private unsafe ExternalObjectsOpenGlExtensionFeature(IGlContext context, List<string> extensions)
    {
        _context = context;
        _ext = new ExternalObjectsInterface(_context.GlInterface.GetProcAddress);

        if (_ext.IsGetUnsignedBytei_vEXTAvailable)
        {
            _context.GlInterface.GetIntegerv(GL_NUM_DEVICE_UUIDS_EXT, out var numUiids);
            if (numUiids > 0)
            {
                DeviceUuid = new byte[16];
                fixed (byte* pUuid = DeviceUuid)
                    _ext.GetUnsignedBytei_vEXT(GL_DEVICE_UUID_EXT, 0, pUuid);
            }
        }

        if (_ext.IsGetUnsignedBytevEXTAvailable)
        {
            if (extensions.Contains("GL_EXT_memory_object_win32") || extensions.Contains("GL_EXT_semaphore_win32"))
            {
                DeviceLuid = new byte[8];
                fixed (byte* pLuid = DeviceLuid)
                    _ext.GetUnsignedBytevEXT(GL_DEVICE_LUID_EXT, pLuid);
            }
        }

        if (extensions.Contains("GL_EXT_memory_object_fd")
            && extensions.Contains("GL_EXT_semaphore_fd"))
        {
            _imageTypes.Add(KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor);
            _semaphoreTypes.Add(KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor);
        }
        


    }

    public IReadOnlyList<string> SupportedImportableExternalImageTypes => _imageTypes;
    public IReadOnlyList<string> SupportedExportableExternalImageTypes { get; } = Array.Empty<string>();
    public IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes => _semaphoreTypes;
    public IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes { get; } = Array.Empty<string>();
    public IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type)
    {
        return new[]
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm
        };
    }

    public IGlExportableExternalImageTexture CreateImage(string type, PixelSize size,
        PlatformGraphicsExternalImageFormat format) =>
        throw new NotSupportedException();

    public IGlExportableExternalImageTexture CreateSemaphore(string type) => throw new NotSupportedException();

    public IGlExternalImageTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        var handleDescriptor = handle.HandleDescriptor;

        if (string.IsNullOrEmpty(handleDescriptor))
            throw new ArgumentException("The handle must have a descriptor", nameof(handle));

        if (!_imageTypes.Contains(handleDescriptor))
            throw new ArgumentException(handleDescriptor + " is not supported", nameof(handle));

        if (handleDescriptor == KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor)
        {
            while (_context.GlInterface.GetError() != 0)
            {
                //Skip existing errors
            }
            _ext.CreateMemoryObjectsEXT(1, out var memoryObject);
            _ext.ImportMemoryFdEXT(memoryObject, properties.MemorySize, GL_HANDLE_TYPE_OPAQUE_FD_EXT,
                handle.Handle.ToInt32());
            
            var err = _context.GlInterface.GetError();
            if (err != 0)
                throw OpenGlException.GetFormattedException("glImportMemoryFdEXT", err);

            _context.GlInterface.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);

            var texture = _context.GlInterface.GenTexture();
            _context.GlInterface.BindTexture(GL_TEXTURE_2D, texture);
            _ext.TexStorageMem2DEXT(GL_TEXTURE_2D, 1, GL_RGBA8, properties.Width, properties.Height,
                memoryObject, properties.MemoryOffset);
            err = _context.GlInterface.GetError();

            _context.GlInterface.BindTexture(GL_TEXTURE_2D, oldTexture);
            if (err != 0)
                throw OpenGlException.GetFormattedException("glTexStorageMem2DEXT", err);
            
            return new ExternalImageTexture(_context, properties, _ext, memoryObject, texture);
        }

        throw new ArgumentException(handleDescriptor + " is not supported", nameof(handle));
    }

    public IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        var handleDescriptor = handle.HandleDescriptor;

        if (string.IsNullOrEmpty(handleDescriptor))
            throw new ArgumentException("The handle must have a descriptor", nameof(handle));

        if (!_semaphoreTypes.Contains(handleDescriptor))
            throw new ArgumentException(handleDescriptor + " is not supported");

        if (handleDescriptor == KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor)
        {
            _ext.GenSemaphoresEXT(1, out var semaphore);
            _ext.ImportSemaphoreFdEXT(semaphore, GL_HANDLE_TYPE_OPAQUE_FD_EXT, handle.Handle.ToInt32());
            return new ExternalSemaphore(_context, _ext, semaphore);
        }
        
        throw new ArgumentException(handleDescriptor + " is not supported", nameof(handle));
    }

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        if (imageHandleType == KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor)
            return CompositionGpuImportedImageSynchronizationCapabilities.Semaphores;
        return default;
    }

    public byte[]? DeviceLuid { get; }
    public byte[]? DeviceUuid { get; }

    private unsafe class ExternalSemaphore : IGlExternalSemaphore
    {
        private readonly IGlContext _context;
        private readonly ExternalObjectsInterface _ext;
        private uint _semaphore;

        public ExternalSemaphore(IGlContext context, ExternalObjectsInterface ext, uint semaphore)
        {
            _context = context;
            _ext = ext;
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if(_context.IsLost)
                return;
            using (_context.EnsureCurrent())
                _ext.DeleteSemaphoresEXT(1, ref _semaphore);
            _semaphore = 0;
        }

        public void WaitSemaphore(IGlExternalImageTexture texture)
        {
            var tex = (ExternalImageTexture)texture;
            var texId = tex.TextureId;
            var srcLayout = GL_LAYOUT_TRANSFER_SRC_EXT;
            _ext.WaitSemaphoreEXT(_semaphore, 0, null, 1, &texId, &srcLayout);
        }
        
        public void SignalSemaphore(IGlExternalImageTexture texture)
        {
            var tex = (ExternalImageTexture)texture;
            var texId = tex.TextureId;
            var dstLayout = 0;
            _ext.SignalSemaphoreEXT(_semaphore, 0, null, 1, &texId, &dstLayout);
        }
    }

    private class ExternalImageTexture : IGlExternalImageTexture
    {
        private readonly IGlContext _context;
        private readonly ExternalObjectsInterface _ext;
        private uint _objectId;

        public ExternalImageTexture(IGlContext context,
            PlatformGraphicsExternalImageProperties properties,
            ExternalObjectsInterface ext, uint objectId, int textureId)
        {
            Properties = properties;
            TextureId = textureId;
            _context = context;
            _ext = ext;
            _objectId = objectId;
        }
        
        public void Dispose()
        {
            if(_context.IsLost)
                return;
            using (_context.EnsureCurrent())
            {
                _context.GlInterface.DeleteTexture(TextureId);
                _ext.DeleteMemoryObjectsEXT(1, ref _objectId);
                _objectId = 0;
            }
        }

        public void AcquireKeyedMutex(uint key) => throw new NotSupportedException();

        public void ReleaseKeyedMutex(uint key) => throw new NotSupportedException();

        public int TextureId { get; }
        public int InternalFormat => GL_RGBA8;
        public PlatformGraphicsExternalImageProperties Properties { get; }
    }
}
