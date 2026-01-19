using System;
using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    // ACT = Ancestor Transform Tracker
    // While we generally avoid dealing with keeping world transforms up to date,
    // we still need it for cases like adorners.
    // Instead of updating world transforms eagerly, we use a subscription model where
    // visuals can subscribe to notifications when any ancestor's world transform changes.
    
    class ActHelper
    {
        public readonly HashSet<Action> AncestorChainTransformSubscribers = new();
        public required Action ParentActSubscriptionAction;
        
        // We keep adorner stuff here too
        public required Action AdornedVisualActSubscriptionAction;
        public bool EnqueuedForAdornerUpdate;
    }
    
    private ActHelper? _actHelper;
    
    private ActHelper GetActHelper() => _actHelper ??= new()
    {
        ParentActSubscriptionAction = ActHelper_CombinedTransformChanged,
        AdornedVisualActSubscriptionAction = ActHelper_OnAdornedVisualWorldTransformChanged
    };

    private void ActHelper_CombinedTransformChanged()
    {
        if(_actHelper == null || _actHelper.AncestorChainTransformSubscribers.Count == 0)
            return;
        foreach (var sub in _actHelper.AncestorChainTransformSubscribers)
            sub();
    }
    
    private void ActHelper_ParentChanging()
    {
        if(Parent != null && _actHelper?.AncestorChainTransformSubscribers.Count > 0)
            Parent.ActHelper_UnsubscribeFromActNotification(_actHelper.ParentActSubscriptionAction);
    }

    private void ActHelper_ParentChanged()
    {
        if(Parent != null && _actHelper?.AncestorChainTransformSubscribers.Count > 0)
            Parent.ActHelper_SubscribeToActNotification(_actHelper.ParentActSubscriptionAction);
        if(Parent != null && AdornedVisual != null)
            AdornerHelper_EnqueueForAdornerUpdate();
    }
    
    protected void ActHelper_SubscribeToActNotification(Action cb)
    {
        var h = GetActHelper();
        
        (h.AncestorChainTransformSubscribers).Add(cb);
        if (h.AncestorChainTransformSubscribers.Count == 1)
            Parent?.ActHelper_SubscribeToActNotification(h.ParentActSubscriptionAction);
    }
    
    protected void ActHelper_UnsubscribeFromActNotification(Action cb)
    {
        var h = GetActHelper();
        h.AncestorChainTransformSubscribers.Remove(cb);
        if(h.AncestorChainTransformSubscribers.Count == 0)
            Parent?.ActHelper_UnsubscribeFromActNotification(h.ParentActSubscriptionAction);
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