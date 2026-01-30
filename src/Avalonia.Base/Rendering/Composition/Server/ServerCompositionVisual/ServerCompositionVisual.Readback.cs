using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    public class ReadbackData
    {
        public Matrix Matrix;
        public ulong Revision;
        public long TargetId;
        public bool Visible;
        public LtrbRect? TransformedSubtreeBounds;
    }
    
    private ReadbackData
        _readback0 = new() { Revision = ulong.MaxValue },
        _readback1 = new() { Revision = ulong.MaxValue };
    
    private bool _enqueuedForReadbackUpdate = false;

    private void EnqueueForReadbackUpdate()
    {
        if (!_enqueuedForReadbackUpdate)
        {
            _enqueuedForReadbackUpdate = true;
            Compositor.EnqueueVisualForReadbackUpdatePass(this);
        }
    }

    public ReadbackData? GetReadback(ulong readerRevision)
    {
        // Prevent ulong tearing
        var slot0Revision = Interlocked.Read(ref _readback0.Revision);
        var slot1Revision = Interlocked.Read(ref _readback1.Revision);

        if (slot0Revision <= readerRevision && slot1Revision <= readerRevision)
        {
            // Pick the newest one, it's guaranteed to be not touched by the writer
            return slot1Revision > slot0Revision ? _readback1 : _readback0;
        }

        if (slot0Revision <= readerRevision)
            return _readback0;
        
        if (slot1Revision <= readerRevision)
            return _readback1;
        
        // No readback was written for this visual yet
        return null;
    }
    
    public void UpdateReadback(ulong writerRevision, ulong readerRevision)
    {
        _enqueuedForReadbackUpdate = false;
        ReadbackData slot;

        if (_readback0.Revision > readerRevision) // Future revision is in slot0
            slot = _readback0;
        else if (_readback1.Revision > readerRevision) // Future revision is in slot0
            slot = _readback1;
        else 
            // No future revisions, overwrite the oldest one since reader will always pick the newest
            slot = (_readback0.Revision < _readback1.Revision) ? _readback0 : _readback1;

        // Prevent ulong tearing
        Interlocked.Exchange(ref slot.Revision, writerRevision);
        slot.Matrix = _ownTransform ?? Matrix.Identity;
        slot.TargetId = Root?.Id ?? -1;
        slot.TransformedSubtreeBounds = _transformedSubTreeBounds;
        slot.Visible = Visible;
    }
    

}