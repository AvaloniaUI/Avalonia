using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Avalonia.DmaBufInteropTests.NativeInterop;

namespace Avalonia.DmaBufInteropTests.Tests;

/// <summary>
/// Tests EGL DMA-BUF import path directly without Avalonia.
/// </summary>
internal static unsafe class EglDmaBufImportTests
{
    public static List<TestResult> Run()
    {
        var results = new List<TestResult>();

        using var allocator = new DmaBufAllocator();
        if (!allocator.IsAvailable)
        {
            results.Add(new TestResult("Egl_DmaBuf_All", TestStatus.Skipped, "no render node available"));
            return results;
        }

        var display = IntPtr.Zero;
        var context = IntPtr.Zero;
        var surface = IntPtr.Zero;

        try
        {
            // Try GBM platform display first, fall back to default
            var getPlatformDisplay = LoadEglExtension<EglGetPlatformDisplayDelegate>("eglGetPlatformDisplayEXT");
            if (getPlatformDisplay != null)
            {
                int drmFd = -1;
                IntPtr gbm = IntPtr.Zero;
                foreach (var path in new[] { "/dev/dri/renderD128", "/dev/dri/renderD129" })
                {
                    if (!System.IO.File.Exists(path)) continue;
                    drmFd = Open(path, O_RDWR);
                    if (drmFd >= 0)
                    {
                        gbm = GbmCreateDevice(drmFd);
                        if (gbm != IntPtr.Zero) break;
                        Close(drmFd);
                        drmFd = -1;
                    }
                }
                if (gbm != IntPtr.Zero)
                    display = getPlatformDisplay(EGL_PLATFORM_GBM_KHR, gbm, null);
            }

            if (display == IntPtr.Zero)
                display = EglGetDisplay(IntPtr.Zero);

            if (display == IntPtr.Zero)
            {
                results.Add(new TestResult("Egl_DmaBuf_All", TestStatus.Skipped, "cannot get EGL display"));
                return results;
            }

            if (!EglInitialize(display, out _, out _))
            {
                results.Add(new TestResult("Egl_DmaBuf_All", TestStatus.Skipped, "eglInitialize failed"));
                return results;
            }

            var extensions = EglQueryString(display, EGL_EXTENSIONS) ?? "";

            results.Add(extensions.Contains("EGL_EXT_image_dma_buf_import")
                ? new TestResult("Egl_DmaBuf_Extension_Availability", TestStatus.Passed)
                : new TestResult("Egl_DmaBuf_Extension_Availability", TestStatus.Skipped,
                    "EGL_EXT_image_dma_buf_import not available"));

            if (!extensions.Contains("EGL_EXT_image_dma_buf_import"))
            {
                EglTerminate(display);
                return results;
            }

            // Create GL context — try pbuffer first, then surfaceless
            EglBindApi(EGL_OPENGL_ES_API);
            IntPtr config;
            int numConfig;
            bool useSurfaceless = false;

            // Try pbuffer config first
            var configAttribs = new[]
            {
                EGL_SURFACE_TYPE, EGL_PBUFFER_BIT,
                EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT,
                EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                EGL_NONE
            };
            EglChooseConfig(display, configAttribs, &config, 1, out numConfig);
            if (numConfig == 0)
            {
                configAttribs[3] = EGL_OPENGL_ES2_BIT;
                EglChooseConfig(display, configAttribs, &config, 1, out numConfig);
            }

            // Fall back to surfaceless (no surface type requirement)
            if (numConfig == 0)
            {
                configAttribs = new[]
                {
                    EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT,
                    EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                    EGL_NONE
                };
                EglChooseConfig(display, configAttribs, &config, 1, out numConfig);
                if (numConfig == 0)
                {
                    configAttribs[1] = EGL_OPENGL_ES2_BIT;
                    EglChooseConfig(display, configAttribs, &config, 1, out numConfig);
                }
                useSurfaceless = numConfig > 0;
            }

            if (numConfig == 0)
            {
                results.Add(new TestResult("Egl_DmaBuf_All", TestStatus.Skipped, "no suitable EGL config"));
                EglTerminate(display);
                return results;
            }

            var ctxAttribs = new[] { EGL_CONTEXT_MAJOR_VERSION, 3, EGL_CONTEXT_MINOR_VERSION, 0, EGL_NONE };
            context = EglCreateContext(display, config, IntPtr.Zero, ctxAttribs);
            if (context == IntPtr.Zero)
            {
                ctxAttribs = new[] { EGL_CONTEXT_MAJOR_VERSION, 2, EGL_CONTEXT_MINOR_VERSION, 0, EGL_NONE };
                context = EglCreateContext(display, config, IntPtr.Zero, ctxAttribs);
            }

            if (context == IntPtr.Zero)
            {
                results.Add(new TestResult("Egl_DmaBuf_All", TestStatus.Skipped, "cannot create EGL context"));
                EglTerminate(display);
                return results;
            }

            if (useSurfaceless)
            {
                // EGL_KHR_surfaceless_context: pass EGL_NO_SURFACE
                EglMakeCurrent(display, IntPtr.Zero, IntPtr.Zero, context);
            }
            else
            {
                surface = EglCreatePbufferSurface(display, config, new[] { EGL_WIDTH, 1, EGL_HEIGHT, 1, EGL_NONE });
                EglMakeCurrent(display, surface, surface, context);
            }

            var eglCreateImageKHR = LoadEglExtension<EglCreateImageKHRDelegate>("eglCreateImageKHR");
            var eglDestroyImageKHR = LoadEglExtension<EglDestroyImageKHRDelegate>("eglDestroyImageKHR");
            var glEGLImageTargetTexture2DOES =
                LoadEglExtension<GlEGLImageTargetTexture2DOESDelegate>("glEGLImageTargetTexture2DOES");

            if (eglCreateImageKHR == null || eglDestroyImageKHR == null || glEGLImageTargetTexture2DOES == null)
            {
                results.Add(new TestResult("Egl_DmaBuf_Image_Import", TestStatus.Skipped,
                    "missing eglCreateImageKHR or glEGLImageTargetTexture2DOES"));
            }
            else
            {
                results.AddRange(TestFormatQuery(display, extensions));
                results.Add(TestImageImportAndReadback(display, allocator, eglCreateImageKHR,
                    eglDestroyImageKHR, glEGLImageTargetTexture2DOES));
            }

            results.Add(TestSyncFence(display, extensions));
        }
        finally
        {
            if (context != IntPtr.Zero)
            {
                EglMakeCurrent(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (surface != IntPtr.Zero)
                    EglDestroySurface(display, surface);
                EglDestroyContext(display, context);
            }

            if (display != IntPtr.Zero)
                EglTerminate(display);
        }

        return results;
    }

    private static List<TestResult> TestFormatQuery(IntPtr display, string extensions)
    {
        if (!extensions.Contains("EGL_EXT_image_dma_buf_import_modifiers"))
            return [new TestResult("Egl_DmaBuf_Format_Query", TestStatus.Skipped,
                "EGL_EXT_image_dma_buf_import_modifiers not available")];

        var queryFormats = LoadEglExtension<EglQueryDmaBufFormatsEXTDelegate>("eglQueryDmaBufFormatsEXT");
        var queryModifiers = LoadEglExtension<EglQueryDmaBufModifiersEXTDelegate>("eglQueryDmaBufModifiersEXT");
        if (queryFormats == null || queryModifiers == null)
            return [new TestResult("Egl_DmaBuf_Format_Query", TestStatus.Skipped, "query functions not available")];

        queryFormats(display, 0, null, out var numFormats);
        if (numFormats == 0)
            return [new TestResult("Egl_DmaBuf_Format_Query", TestStatus.Failed,
                "eglQueryDmaBufFormatsEXT returned 0 formats")];

        var formats = new int[numFormats];
        queryFormats(display, numFormats, formats, out _);

        bool hasArgb8888 = false;
        foreach (var fmt in formats)
            if ((uint)fmt == GBM_FORMAT_ARGB8888)
                hasArgb8888 = true;

        return [hasArgb8888
            ? new TestResult("Egl_DmaBuf_Format_Query", TestStatus.Passed,
                $"{numFormats} formats, ARGB8888 supported")
            : new TestResult("Egl_DmaBuf_Format_Query", TestStatus.Failed,
                $"{numFormats} formats but DRM_FORMAT_ARGB8888 not found")];
    }

    private static TestResult TestImageImportAndReadback(IntPtr display, DmaBufAllocator allocator,
        EglCreateImageKHRDelegate eglCreateImageKHR, EglDestroyImageKHRDelegate eglDestroyImageKHR,
        GlEGLImageTargetTexture2DOESDelegate glEGLImageTargetTexture2DOES)
    {
        const uint width = 64, height = 64;
        const uint greenColor = 0xFF00FF00; // ARGB: full green

        using var alloc = allocator.AllocateLinear(width, height, GBM_FORMAT_ARGB8888, greenColor);
        if (alloc == null)
            return new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Skipped,
                "could not allocate DMA-BUF");

        var attribs = new[]
        {
            EGL_WIDTH, (int)width,
            EGL_HEIGHT, (int)height,
            EGL_LINUX_DRM_FOURCC_EXT, (int)GBM_FORMAT_ARGB8888,
            EGL_DMA_BUF_PLANE0_FD_EXT, alloc.Fd,
            EGL_DMA_BUF_PLANE0_OFFSET_EXT, 0,
            EGL_DMA_BUF_PLANE0_PITCH_EXT, (int)alloc.Stride,
            EGL_DMA_BUF_PLANE0_MODIFIER_LO_EXT, (int)(alloc.Modifier & 0xFFFFFFFF),
            EGL_DMA_BUF_PLANE0_MODIFIER_HI_EXT, (int)(alloc.Modifier >> 32),
            EGL_NONE
        };

        var eglImage = eglCreateImageKHR(display, IntPtr.Zero, EGL_LINUX_DMA_BUF_EXT, IntPtr.Zero, attribs);
        if (eglImage == IntPtr.Zero)
        {
            var err = EglGetError();
            return new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Failed,
                $"eglCreateImageKHR failed with 0x{err:X}");
        }

        try
        {
            int texId;
            GlGenTextures(1, &texId);
            GlBindTexture(GL_TEXTURE_2D, texId);
            glEGLImageTargetTexture2DOES(GL_TEXTURE_2D, eglImage);

            var glErr = GlGetError();
            if (glErr != 0)
            {
                GlDeleteTextures(1, &texId);
                return new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Failed,
                    $"glEGLImageTargetTexture2DOES error: 0x{glErr:X}");
            }

            int fbo;
            GlGenFramebuffers(1, &fbo);
            GlBindFramebuffer(GL_FRAMEBUFFER, fbo);
            GlFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texId, 0);

            var status = GlCheckFramebufferStatus(GL_FRAMEBUFFER);
            if (status != GL_FRAMEBUFFER_COMPLETE)
            {
                GlBindFramebuffer(GL_FRAMEBUFFER, 0);
                GlDeleteFramebuffers(1, &fbo);
                GlDeleteTextures(1, &texId);
                return new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Failed,
                    $"framebuffer incomplete: 0x{status:X}");
            }

            var pixels = new byte[width * height * 4];
            fixed (byte* pPixels = pixels)
                GlReadPixels(0, 0, (int)width, (int)height, GL_RGBA, GL_UNSIGNED_BYTE, pPixels);

            GlBindFramebuffer(GL_FRAMEBUFFER, 0);
            GlDeleteFramebuffers(1, &fbo);
            GlDeleteTextures(1, &texId);

            // Verify: DRM_FORMAT_ARGB8888 with green=0xFF → G=0xFF, A=0xFF after import
            int correctPixels = 0;
            for (int i = 0; i < width * height; i++)
            {
                var g = pixels[i * 4 + 1];
                var a = pixels[i * 4 + 3];
                if (g == 0xFF && a == 0xFF)
                    correctPixels++;
            }

            var correctRatio = (double)correctPixels / (width * height);
            return correctRatio > 0.95
                ? new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Passed,
                    $"{correctRatio:P0} pixels correct")
                : new TestResult("Egl_DmaBuf_Image_Import_And_Readback", TestStatus.Failed,
                    $"only {correctRatio:P0} pixels correct (expected >95%)");
        }
        finally
        {
            eglDestroyImageKHR(display, eglImage);
        }
    }

    private static TestResult TestSyncFence(IntPtr display, string extensions)
    {
        if (!extensions.Contains("EGL_ANDROID_native_fence_sync"))
            return new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Skipped,
                "EGL_ANDROID_native_fence_sync not available");

        var createSync = LoadEglExtension<EglCreateSyncKHRDelegate>("eglCreateSyncKHR");
        var destroySync = LoadEglExtension<EglDestroySyncKHRDelegate>("eglDestroySyncKHR");
        var clientWait = LoadEglExtension<EglClientWaitSyncKHRDelegate>("eglClientWaitSyncKHR");
        var dupFence = LoadEglExtension<EglDupNativeFenceFDANDROIDDelegate>("eglDupNativeFenceFDANDROID");

        if (createSync == null || destroySync == null || clientWait == null || dupFence == null)
            return new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Skipped,
                "sync fence functions not available");

        GlFlush();

        var sync = createSync(display, EGL_SYNC_NATIVE_FENCE_ANDROID,
            new[] { EGL_SYNC_NATIVE_FENCE_FD_ANDROID, EGL_NO_NATIVE_FENCE_FD_ANDROID, EGL_NONE });
        if (sync == IntPtr.Zero)
        {
            var err = EglGetError();
            return new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Failed,
                $"eglCreateSyncKHR (export) failed with 0x{err:X}");
        }

        var fd = dupFence(display, sync);
        destroySync(display, sync);

        if (fd < 0)
            return new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Failed,
                $"eglDupNativeFenceFDANDROID returned {fd}");

        var importSync = createSync(display, EGL_SYNC_NATIVE_FENCE_ANDROID,
            new[] { EGL_SYNC_NATIVE_FENCE_FD_ANDROID, fd, EGL_NONE });
        if (importSync == IntPtr.Zero)
        {
            var err = EglGetError();
            return new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Failed,
                $"eglCreateSyncKHR (import) failed with 0x{err:X}");
        }

        var waitResult = clientWait(display, importSync, EGL_SYNC_FLUSH_COMMANDS_BIT_KHR, EGL_FOREVER_KHR);
        destroySync(display, importSync);

        return waitResult == EGL_CONDITION_SATISFIED_KHR
            ? new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Passed)
            : new TestResult("Egl_SyncFence_RoundTrip", TestStatus.Failed,
                $"eglClientWaitSyncKHR returned 0x{waitResult:X}");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr EglGetPlatformDisplayDelegate(int platform, IntPtr nativeDisplay, int[]? attribs);
}
