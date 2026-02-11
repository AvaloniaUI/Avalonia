using System;
using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    // ATT = Ancestor Transform Tracker
    // While we generally avoid dealing with keeping world transforms up to date,
    // we still need it for cases like adorners.
    // Instead of updating world transforms eagerly, we use a subscription model where
    // visuals can subscribe to notifications when any ancestor's world transform changes.
    
    class AttHelper
    {
        public readonly HashSet<Action> AncestorChainTransformSubscribers = new();
        public required Action ParentActSubscriptionAction;
        
        // We keep adorner stuff here too
        public required Action AdornedVisualActSubscriptionAction;
        public bool EnqueuedForAdornerUpdate;
    }
    
    private AttHelper? _AttHelper;
    
    private AttHelper GetAttHelper() => _AttHelper ??= new()
    {
        ParentActSubscriptionAction = AttHelper_CombinedTransformChanged,
        AdornedVisualActSubscriptionAction = AttHelper_OnAdornedVisualWorldTransformChanged
    };

    private void AttHelper_CombinedTransformChanged()
    {
        if(_AttHelper == null || _AttHelper.AncestorChainTransformSubscribers.Count == 0)
            return;
        foreach (var sub in _AttHelper.AncestorChainTransformSubscribers)
            sub();
    }
    
    private void AttHelper_ParentChanging()
    {
        if(Parent != null && _AttHelper?.AncestorChainTransformSubscribers.Count > 0)
            Parent.AttHelper_UnsubscribeFromActNotification(_AttHelper.ParentActSubscriptionAction);
    }

    private void AttHelper_ParentChanged()
    {
        if(Parent != null && _AttHelper?.AncestorChainTransformSubscribers.Count > 0)
            Parent.AttHelper_SubscribeToActNotification(_AttHelper.ParentActSubscriptionAction);
        if(Parent != null && AdornedVisual != null)
            AdornerHelper_EnqueueForAdornerUpdate();
    }
    
    protected void AttHelper_SubscribeToActNotification(Action cb)
    {
        var h = GetAttHelper();
        
        (h.AncestorChainTransformSubscribers).Add(cb);
        if (h.AncestorChainTransformSubscribers.Count == 1)
            Parent?.AttHelper_SubscribeToActNotification(h.ParentActSubscriptionAction);
    }
    
    protected void AttHelper_UnsubscribeFromActNotification(Action cb)
    {
        var h = GetAttHelper();
        h.AncestorChainTransformSubscribers.Remove(cb);
        if(h.AncestorChainTransformSubscribers.Count == 0)
            Parent?.AttHelper_UnsubscribeFromActNotification(h.ParentActSubscriptionAction);
    }
    
    protected static bool ComputeTransformFromAncestor(ServerCompositionVisual visual,
        ServerCompositionVisual ancestor, out Matrix transform)
    {
        transform = visual._ownTransform ?? Matrix.Identity;
        while (visual.Parent != null)
        {
            visual = visual.Parent;
            
            if (visual == ancestor) // Walked up to ancestor
                return true;
            
            if (visual._ownTransform.HasValue)
                transform = transform * visual._ownTransform.Value;
        }

        // Visual is a part of a different subtree, this is not supported
        return false;
    }
}