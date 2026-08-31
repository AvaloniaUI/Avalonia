using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionDrawingSurfaceTests : ScopedTestBase
{
    private class TrackingBitmapImpl : IBitmapImpl
    {
        public bool IsDisposed { get; private set; }
        public Vector Dpi => new(96, 96);
        public PixelSize PixelSize => new(1, 1);
        public int Version => 1;
        public void Save(Stream stream, BitmapEncoderOptions options) => throw new NotSupportedException();
        public void Dispose() => IsDisposed = true;
    }

    private class FakeImportedImage : IPlatformRenderInterfaceImportedImage
    {
        public TrackingBitmapImpl LastSnapshot { get; private set; } = null!;

        private IBitmapImpl Snapshot() => LastSnapshot = new TrackingBitmapImpl();

        public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex) => Snapshot();
        public IBitmapImpl SnapshotWithSemaphores(
            IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
            IPlatformRenderInterfaceImportedSemaphore signalSemaphore) => Snapshot();
        public IBitmapImpl SnapshotWithTimelineSemaphores(
            IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, ulong waitForValue,
            IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue) => Snapshot();
        public IBitmapImpl SnapshotWithAutomaticSync() => Snapshot();
        public void Dispose()
        {
        }
    }

    private class FakeExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature
    {
        public FakeImportedImage Image { get; } = new();

        public IReadOnlyList<string> SupportedImageHandleTypes => new[] { "Fake" };
        public IReadOnlyList<string> SupportedSemaphoreTypes => Array.Empty<string>();
        public byte[]? DeviceUuid => null;
        public byte[]? DeviceLuid => null;

        public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
            PlatformGraphicsExternalImageProperties properties) => Image;

        public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image) =>
            Image;

        public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle) =>
            throw new NotSupportedException();

        public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(
            string imageHandleType) => CompositionGpuImportedImageSynchronizationCapabilities.Automatic;
    }

    [Fact]
    public async Task Update_Processed_After_Dispose_Should_Dispose_Snapshot_Instead_Of_Orphaning_It()
    {
        // A commit batch is processed on the render thread in serialization order:
        // the dispose list is written (and therefore processed) BEFORE queued server
        // jobs. This means the perfectly legal user-code order
        //     surface.UpdateAsync(image); surface.Dispose();
        // executes on the render thread as Dispose() -> UpdateWithAutomaticSync().
        // Without a disposed-guard, the update stores a fresh snapshot into the
        // already-disposed surface. Nothing ever disposes that ref again, so the
        // GPU-backed snapshot is left to the RefCountable.Ref<T> critical finalizer,
        // which performs GPU work (context MakeCurrent + native SKImage dispose) on
        // the finalizer thread and races the compositor render loop. See #21865.
        using var services = new CompositorTestServices();
        var compositor = services.Compositor;

        var feature = new FakeExternalObjectsFeature();
        var interop = new CompositionInterop(compositor, feature);
        var imported = interop.ImportImage(
            new PlatformHandle(new IntPtr(1), "Fake"),
            new PlatformGraphicsExternalImageProperties { Width = 1, Height = 1 });

        services.RunJobs();
        Assert.True(((CompositionImportedGpuImage)imported).ImportCompleted.IsCompletedSuccessfully);

        var surface = compositor.CreateDrawingSurface();

        var update = surface.UpdateAsync(imported);
        surface.Dispose();

        services.RunJobs();
        await update;

        var snapshot = feature.Image.LastSnapshot;
        Assert.NotNull(snapshot);
        Assert.True(snapshot.IsDisposed,
            "The snapshot taken by an update processed after the surface was disposed " +
            "must be disposed on the render thread instead of being orphaned to the finalizer.");
    }

    [Fact]
    public async Task Update_Before_Dispose_In_Separate_Batches_Should_Dispose_Snapshot_With_The_Surface()
    {
        // Baseline: when the update is processed in an earlier batch than the dispose,
        // the surface's Dispose() releases the stored snapshot. This already works and
        // must keep working with the disposed-guard in place.
        using var services = new CompositorTestServices();
        var compositor = services.Compositor;

        var feature = new FakeExternalObjectsFeature();
        var interop = new CompositionInterop(compositor, feature);
        var imported = interop.ImportImage(
            new PlatformHandle(new IntPtr(1), "Fake"),
            new PlatformGraphicsExternalImageProperties { Width = 1, Height = 1 });

        services.RunJobs();

        var surface = compositor.CreateDrawingSurface();

        var update = surface.UpdateAsync(imported);
        services.RunJobs();
        await update;

        var snapshot = feature.Image.LastSnapshot;
        Assert.NotNull(snapshot);
        Assert.False(snapshot.IsDisposed);

        surface.Dispose();
        services.RunJobs();

        Assert.True(snapshot.IsDisposed);
    }
}
