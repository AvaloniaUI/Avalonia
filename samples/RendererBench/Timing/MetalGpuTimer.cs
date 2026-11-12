using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Metal;

namespace RendererBench.Timing;

/// <summary>
/// Measures GPU time of a frame on Metal from timestamps sampled at the start and end of each render encoder.
/// </summary>
/// <remarks>
/// Neither backend exposes its command buffers (Impeller even submits on a private queue), so
/// -[MTLCommandBuffer commit] and -renderCommandEncoderWithDescriptor: are replaced process-wide. A buffer
/// belongs to the last frame that began before its commit. The frame's GPU time is the union of its encoders'
/// GPU intervals, reported once all of its buffers completed and the next frame began. A command buffer's
/// GPUStartTime/GPUEndTime is not used: it also covers time the GPU spends on other work.
/// </remarks>
internal sealed unsafe class MetalGpuTimer : IGpuTimer
{
    private const long StatusCompleted = 4;

    private const string ObjC = "/usr/lib/libobjc.dylib";

    // A sample buffer holds at most 32 KB of timestamps, so several are used as one ring.
    private const int SamplesPerBuffer = 4096;
    private const int SampleBufferCount = 4;
    private const int SampleCount = SamplesPerBuffer * SampleBufferCount;
    private static readonly nuint DontSample = nuint.MaxValue;

    private static readonly object s_lock = new();
    private static readonly ConcurrentDictionary<(IntPtr Class, IntPtr Sel), IntPtr> s_originals = new();
    private static readonly List<(IntPtr Buffer, long FrameId, List<int>? Slots, ulong CommitGpu)> s_pending = new();
    private static readonly Dictionary<IntPtr, List<int>> s_encoderSlots = new();
    private static readonly Dictionary<long, List<(double Start, double End)>> s_done = new();
    private static long s_frame = -1;
    private static long s_nextSlot;
    private static IntPtr s_device;
    private static IntPtr[]? s_sampleBuffers;
    private static (ulong Cpu, ulong Gpu) s_clockStart;
    private static double s_gpuTicksToMs = 1e-6;

    public string Method => "encoder-timestamps";

    public static MetalGpuTimer Create(IMetalDevice device)
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            s_device = device.Device;
            if (s_sampleBuffers is null)
            {
                var buffers = new IntPtr[SampleBufferCount];
                for (var i = 0; i < buffers.Length; i++)
                {
                    buffers[i] = CreateSampleBuffer(s_device);
                    if (buffers[i] == IntPtr.Zero)
                        throw new NotSupportedException(
                            "The Metal device can't sample timestamps at stage boundaries.");
                }
                s_sampleBuffers = buffers;
            }
            ulong cpu, gpu;
            SampleTimestamps(s_device, Sel("sampleTimestamps:gpuTimestamp:"), &cpu, &gpu);
            s_clockStart = (cpu, gpu);

            // The buffer class depends on how it is created, so hook every flavor the backends use.
            var queue = device.CommandQueue;
            HookBuffer(object_getClass(IntPtr_msgSend(queue, Sel("commandBuffer"))));
            HookBuffer(object_getClass(IntPtr_msgSend(queue, Sel("commandBufferWithUnretainedReferences"))));
            var descriptor = IntPtr_msgSend(IntPtr_msgSend(objc_getClass("MTLCommandBufferDescriptor"),
                Sel("alloc")), Sel("init"));
            HookBuffer(object_getClass(IntPtr_msgSend(queue, Sel("commandBufferWithDescriptor:"), descriptor)));
            IntPtr_msgSend(descriptor, Sel("release"));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }

        return new MetalGpuTimer();
    }

    public void Begin(long frameId)
    {
        Poll(frameId);
        Volatile.Write(ref s_frame, frameId);
    }

    public double? End() => null;

    // A shared buffer of GPU timestamps, or zero when the device can't sample them at stage boundaries.
    private static IntPtr CreateSampleBuffer(IntPtr device)
    {
        if (!Bool_msgSend(device, Sel("supportsCounterSampling:"), 0 /* AtStageBoundary */))
            return IntPtr.Zero;

        var sets = IntPtr_msgSend(device, Sel("counterSets"));
        var count = (long)IntPtr_msgSend(sets, Sel("count"));
        for (long i = 0; i < count; i++)
        {
            var set = IntPtr_msgSend(sets, Sel("objectAtIndex:"), (IntPtr)i);
            var name = Marshal.PtrToStringUTF8(IntPtr_msgSend(IntPtr_msgSend(set, Sel("name")), Sel("UTF8String")));
            if (name != "timestamp")
                continue;

            var descriptor = IntPtr_msgSend(IntPtr_msgSend(objc_getClass("MTLCounterSampleBufferDescriptor"),
                Sel("alloc")), Sel("init"));
            IntPtr_msgSend(descriptor, Sel("setCounterSet:"), set);
            Void_msgSend(descriptor, Sel("setStorageMode:"), 0 /* Shared */);
            Void_msgSend(descriptor, Sel("setSampleCount:"), SamplesPerBuffer);
            IntPtr error;
            var buffer = IntPtr_msgSend(device, Sel("newCounterSampleBufferWithDescriptor:error:"), descriptor,
                (IntPtr)(&error));
            IntPtr_msgSend(descriptor, Sel("release"));
            return buffer;
        }

        return IntPtr.Zero;
    }

    private static void HookBuffer(IntPtr cls)
    {
        Hook(cls, Sel("commit"), (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&Commit);
        Hook(cls, Sel("renderCommandEncoderWithDescriptor:"),
            (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr>)&RenderEncoder);
    }

    private static void Hook(IntPtr cls, IntPtr sel, IntPtr hook)
    {
        lock (s_lock)
        {
            if (s_originals.ContainsKey((cls, sel)))
                return;

            var method = class_getInstanceMethod(cls, sel);
            var original = method_getImplementation(method);

            // Inherited from a class that is already hooked: Original finds that class's original.
            if (original == hook)
                return;

            s_originals[(cls, sel)] = original;
            if (!class_addMethod(cls, sel, hook, method_getTypeEncoding(method)))
                method_setImplementation(method, hook);
        }
    }

    private static IntPtr Original(IntPtr self, IntPtr sel)
    {
        var cls = object_getClass(self);
        IntPtr original;
        while (!s_originals.TryGetValue((cls, sel), out original))
            cls = class_getSuperclass(cls);
        return original;
    }

    [UnmanagedCallersOnly]
    private static void Commit(IntPtr self, IntPtr cmd)
    {
        IntPtr_msgSend(self, Sel("retain"));
        var commitGpu = GpuNow();
        lock (s_lock)
        {
            s_encoderSlots.Remove(self, out var slots);
            s_pending.Add((self, Volatile.Read(ref s_frame), slots, commitGpu));
        }

        ((delegate* unmanaged<IntPtr, IntPtr, void>)Original(self, cmd))(self, cmd);
    }

    // Samples a timestamp when the encoder's vertex stage starts and when its fragment stage ends.
    [UnmanagedCallersOnly]
    private static IntPtr RenderEncoder(IntPtr self, IntPtr cmd, IntPtr descriptor)
    {
        if (descriptor != IntPtr.Zero)
        {
            var slot = (int)((Interlocked.Add(ref s_nextSlot, 2) - 2) % SampleCount);
            var attachment = IntPtr_msgSend(IntPtr_msgSend(descriptor, Sel("sampleBufferAttachments")),
                Sel("objectAtIndexedSubscript:"), IntPtr.Zero);
            var index = slot % SamplesPerBuffer;
            IntPtr_msgSend(attachment, Sel("setSampleBuffer:"), s_sampleBuffers![slot / SamplesPerBuffer]);
            Void_msgSend(attachment, Sel("setStartOfVertexSampleIndex:"), (nuint)index);
            Void_msgSend(attachment, Sel("setEndOfVertexSampleIndex:"), DontSample);
            Void_msgSend(attachment, Sel("setStartOfFragmentSampleIndex:"), DontSample);
            Void_msgSend(attachment, Sel("setEndOfFragmentSampleIndex:"), (nuint)(index + 1));
            lock (s_lock)
            {
                if (!s_encoderSlots.TryGetValue(self, out var slots))
                    s_encoderSlots[self] = slots = new List<int>();
                slots.Add(slot);
            }
        }

        return ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr>)Original(self, cmd))(self, cmd, descriptor);
    }

    private static void Poll(long currentFrame)
    {
        var results = new List<(long FrameId, double Ms)>();
        var pool = objc_autoreleasePoolPush();
        lock (s_lock)
        {
            var gpuNow = UpdateClockRatio();
            for (var i = s_pending.Count - 1; i >= 0; i--)
            {
                var (buffer, frameId, slots, commitGpu) = s_pending[i];
                if (IntPtr_msgSend(buffer, Sel("status")) < StatusCompleted)
                    continue;

                if (frameId >= 0)
                {
                    if (!s_done.TryGetValue(frameId, out var intervals))
                        s_done[frameId] = intervals = new List<(double, double)>();
                    if (slots is not null)
                        AddEncoderIntervals(slots, commitGpu, gpuNow, intervals);
                }

                IntPtr_msgSend(buffer, Sel("release"));
                s_pending.RemoveAt(i);
            }

            foreach (var frameId in s_done.Keys.ToArray())
            {
                if (frameId >= currentFrame || s_pending.Any(p => p.FrameId == frameId))
                    continue;
                if (s_done[frameId].Count > 0)
                    results.Add((frameId, BusyMs(s_done[frameId])));
                s_done.Remove(frameId);
            }
        }
        objc_autoreleasePoolPop(pool);

        foreach (var r in results)
            FrameRecorder.Instance.ReportGpu(r.FrameId, r.Ms);
    }

    // An encoder that didn't sample leaves an earlier frame's values in its slot; those fall outside the
    // time between the commit and now.
    private static void AddEncoderIntervals(List<int> slots, ulong commitGpu, ulong gpuNow,
        List<(double Start, double End)> intervals)
    {
        foreach (var slot in slots)
        {
            var data = Range_msgSend(s_sampleBuffers![slot / SamplesPerBuffer], Sel("resolveCounterRange:"),
                (nuint)(slot % SamplesPerBuffer), 2);
            if (data == IntPtr.Zero || (long)IntPtr_msgSend(data, Sel("length")) < 16)
                continue;
            var samples = (ulong*)IntPtr_msgSend(data, Sel("bytes"));
            var (start, end) = (samples[0], samples[1]);
            if (start < commitGpu || end <= start || end > gpuNow)
                continue;
            intervals.Add((start * s_gpuTicksToMs, end * s_gpuTicksToMs));
        }
    }

    private static ulong GpuNow()
    {
        ulong cpu, gpu;
        SampleTimestamps(s_device, Sel("sampleTimestamps:gpuTimestamp:"), &cpu, &gpu);
        return gpu;
    }

    // GPU timestamps are in device ticks; the CPU side of sampleTimestamps is in nanoseconds.
    private static ulong UpdateClockRatio()
    {
        ulong cpu, gpu;
        SampleTimestamps(s_device, Sel("sampleTimestamps:gpuTimestamp:"), &cpu, &gpu);
        if (gpu - s_clockStart.Gpu > 100_000_000)
            s_gpuTicksToMs = (double)(cpu - s_clockStart.Cpu) / (gpu - s_clockStart.Gpu) * 1e-6;
        return gpu;
    }

    private static double BusyMs(List<(double Start, double End)> intervals)
    {
        intervals.Sort((a, b) => a.Start.CompareTo(b.Start));
        double busy = 0, spanStart = intervals[0].Start, spanEnd = intervals[0].End;
        foreach (var (start, end) in intervals.Skip(1))
        {
            if (start > spanEnd)
            {
                busy += spanEnd - spanStart;
                spanStart = start;
            }
            spanEnd = Math.Max(spanEnd, end);
        }
        return busy + spanEnd - spanStart;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr Sel(string name) => sel_registerName(name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr object_getClass(IntPtr obj);

    [DllImport(ObjC)]
    private static extern IntPtr class_getSuperclass(IntPtr cls);

    [DllImport(ObjC)]
    private static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr sel);

    [DllImport(ObjC)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, IntPtr types);

    [DllImport(ObjC)]
    private static extern IntPtr method_getImplementation(IntPtr method);

    [DllImport(ObjC)]
    private static extern IntPtr method_setImplementation(IntPtr method, IntPtr imp);

    [DllImport(ObjC)]
    private static extern IntPtr method_getTypeEncoding(IntPtr method);

    [DllImport(ObjC)]
    private static extern IntPtr objc_autoreleasePoolPush();

    [DllImport(ObjC)]
    private static extern void objc_autoreleasePoolPop(IntPtr pool);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Bool_msgSend(IntPtr receiver, IntPtr selector, nuint arg);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void Void_msgSend(IntPtr receiver, IntPtr selector, nuint arg);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Range_msgSend(IntPtr receiver, IntPtr selector, nuint location, nuint length);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SampleTimestamps(IntPtr receiver, IntPtr selector, ulong* cpu, ulong* gpu);


}
