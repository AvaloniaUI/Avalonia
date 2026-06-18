using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    // Here we are using a simplified version Multi-Version Concurrency Control with only one reader
    // and only one writer.
    //
    // The goal is to provide un-teared view of a particular revision for the UI thread
    //
    // We are taking a shared lock before switching reader's revision and are using the same lock
    // to produce a new revision, so we know for sure that reader can't switch to a newer revision
    // while we are writing.
    //
    // Reader's behavior: 
    // 1) reader will only pick slots with revision <= its current revision
    // 2) reader will pick the newest revision among slots from (1)
    // There are two scenarios that can be encountered by the writer:
    // 1) both slots contain data for revisions older than the reader's current revision,
    //    in that case we pick the slot with the oldest revision and update it.
    //    1.1) if reader comes before update it will pick the newer one
    //    1.2) if reader comes after update, the overwritten slot would have a revision that's higher than the reader's
    //         one, so it will still pick the same slot
    // 2) one of the slots contains data for a revision newer than the reader's current revision. In that case
    //    we simply pick the slot with revision the reader isn't allowed to touch anyway.
    //    Both before and after update the reader will see only one (same) slot it's allowed to touch
    //
    // While having to hold a lock for the entire time we are writing the revision may seem suboptimal,
    // the UI thread isn't likely to contend for that lock and we update pre-enqueued visuals, so it won't take much time.
    
    
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
        
        // We don't need to use Interlocked.Read here since we are the only writer
        
        if (_readback0.Revision > readerRevision) // Future revision is in slot0
            slot = _readback0;
        else if (_readback1.Revision > readerRevision) // Future revision is in slot1
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