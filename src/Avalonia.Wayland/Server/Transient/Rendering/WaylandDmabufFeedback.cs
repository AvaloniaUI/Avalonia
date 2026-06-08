using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWayland;
using NWayland.Protocols.LinuxDmabufV1;
using static Avalonia.Wayland.Server.Interop.UnsafeNativeMethods;

namespace Avalonia.Wayland.Server.Transient.Rendering;

record struct DmabufFormatModifierPair(uint Format, ulong Modifier);

class DmabufTranche
{
    public ulong TargetDevice { get; set; }
    public List<DmabufFormatModifierPair> Formats { get; } = new();
    public bool Scanout { get; set; }
}

class WaylandDmabufFeedback
{
    const int PROT_READ = 1;
    const int MAP_PRIVATE = 2;
    const int FormatTableEntrySize = 16;

    readonly List<DmabufFormatModifierPair> _formatTable = new();

    ulong _pendingTargetDevice;
    List<DmabufFormatModifierPair> _pendingFormats = new();
    bool _pendingScanout;

    public ulong MainDevice { get; private set; }
    public List<DmabufTranche> Tranches { get; } = new();
    public bool IsComplete { get; private set; }
    public FeedbackListener Listener { get; }

    public WaylandDmabufFeedback()
    {
        Listener = new FeedbackListener(this);
    }

    void ResetPendingTranche()
    {
        _pendingTargetDevice = 0;
        _pendingFormats = new List<DmabufFormatModifierPair>();
        _pendingScanout = false;
    }

    internal unsafe class FeedbackListener(WaylandDmabufFeedback feedback) : ZwpLinuxDmabufFeedbackV1.Listener
    {
        protected override void FormatTable(ZwpLinuxDmabufFeedbackV1 eventSender, WaylandFd fd, uint size)
        {
            var rawFd = fd.Consume();
            var length = (IntPtr)size;
            var map = mmap(IntPtr.Zero, length, PROT_READ, MAP_PRIVATE, rawFd, IntPtr.Zero);
            try
            {
                if (map == (IntPtr)(-1))
                    return;

                var count = (int)(size / FormatTableEntrySize);
                var ptr = (byte*)map;
                feedback._formatTable.Clear();
                for (var i = 0; i < count; i++)
                {
                    var entryPtr = ptr + i * FormatTableEntrySize;
                    var format = *(uint*)entryPtr;
                    var modifier = *(ulong*)(entryPtr + 8);
                    feedback._formatTable.Add(new DmabufFormatModifierPair(format, modifier));
                }
            }
            finally
            {
                munmap(map, length);
                close(rawFd);
            }
        }

        protected override void MainDevice(ZwpLinuxDmabufFeedbackV1 eventSender, ReadOnlySpan<byte> device)
        {
            feedback.MainDevice = MemoryMarshal.Read<ulong>(device);
        }

        protected override void TrancheTargetDevice(ZwpLinuxDmabufFeedbackV1 eventSender, ReadOnlySpan<byte> device)
        {
            feedback._pendingTargetDevice = MemoryMarshal.Read<ulong>(device);
        }

        protected override void TrancheFormats(ZwpLinuxDmabufFeedbackV1 eventSender, ReadOnlySpan<byte> indices)
        {
            var count = indices.Length / 2;
            var span = MemoryMarshal.Cast<byte, ushort>(indices);
            for (var i = 0; i < count; i++)
            {
                var index = span[i];
                if (index < feedback._formatTable.Count)
                    feedback._pendingFormats.Add(feedback._formatTable[index]);
            }
        }

        protected override void TrancheFlags(ZwpLinuxDmabufFeedbackV1 eventSender,
            ZwpLinuxDmabufFeedbackV1.TrancheFlagsEnum flags)
        {
            feedback._pendingScanout = flags.HasFlag(ZwpLinuxDmabufFeedbackV1.TrancheFlagsEnum.Scanout);
        }

        protected override void TrancheDone(ZwpLinuxDmabufFeedbackV1 eventSender)
        {
            var tranche = new DmabufTranche
            {
                TargetDevice = feedback._pendingTargetDevice,
                Scanout = feedback._pendingScanout
            };
            tranche.Formats.AddRange(feedback._pendingFormats);
            feedback.Tranches.Add(tranche);
            feedback.ResetPendingTranche();
        }

        protected override void Done(ZwpLinuxDmabufFeedbackV1 eventSender)
        {
            feedback.IsComplete = true;
        }
    }
}
