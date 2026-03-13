using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.DmaBufInteropTests.Tests;

/// <summary>
/// Pure logic tests — no GPU needed.
/// </summary>
internal static class DrmFormatMappingTests
{
    public static IEnumerable<TestResult> Run()
    {
        yield return Test("DrmFormatMapping_Argb8888_Maps_To_B8G8R8A8",
            PlatformGraphicsDrmFormats.TryMapDrmFormat(PlatformGraphicsDrmFormats.DRM_FORMAT_ARGB8888)
            == PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm);

        yield return Test("DrmFormatMapping_Xrgb8888_Maps_To_B8G8R8A8",
            PlatformGraphicsDrmFormats.TryMapDrmFormat(PlatformGraphicsDrmFormats.DRM_FORMAT_XRGB8888)
            == PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm);

        yield return Test("DrmFormatMapping_Abgr8888_Maps_To_R8G8B8A8",
            PlatformGraphicsDrmFormats.TryMapDrmFormat(PlatformGraphicsDrmFormats.DRM_FORMAT_ABGR8888)
            == PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm);

        yield return Test("DrmFormatMapping_Xbgr8888_Maps_To_R8G8B8A8",
            PlatformGraphicsDrmFormats.TryMapDrmFormat(PlatformGraphicsDrmFormats.DRM_FORMAT_XBGR8888)
            == PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm);

        yield return Test("DrmFormatMapping_Unknown_Returns_Null",
            PlatformGraphicsDrmFormats.TryMapDrmFormat(0x00000000) == null);

        yield return Test("DmaBufFileDescriptor_Is_DMABUF_FD",
            KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor == "DMABUF_FD");

        yield return Test("SyncFileDescriptor_Is_SYNC_FD",
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor == "SYNC_FD");

        yield return Test("DrmModLinear_Is_Zero",
            PlatformGraphicsDrmFormats.DRM_FORMAT_MOD_LINEAR == 0);
    }

    private static TestResult Test(string name, bool condition)
    {
        return condition
            ? new TestResult(name, TestStatus.Passed)
            : new TestResult(name, TestStatus.Failed, "assertion failed");
    }
}
