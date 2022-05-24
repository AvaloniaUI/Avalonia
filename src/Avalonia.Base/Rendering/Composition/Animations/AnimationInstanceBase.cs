using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

internal abstract class AnimationInstanceBase : IAnimationInstance
{
    private List<(ServerObject obj, int member)>? _trackedObjects;
    protected PropertySetSnapshot Parameters { get; }
    public ServerObject TargetObject { get; }
    protected int StoreOffset { get; private set; }
    private bool _invalidated;

    public AnimationInstanceBase(ServerObject target, PropertySetSnapshot parameters)
    {
        Parameters = parameters;
        TargetObject = target;
    }

    protected void Initialize(int storeOffset, HashSet<(string name, string member)> trackedObjects)
    {
        if (trackedObjects.Count > 0)
        {
            _trackedObjects = new ();
            foreach (var t in trackedObjects)
            {
                var obj = Parameters.GetObjectParameter(t.name);
                if (obj is ServerObject tracked)
                {
                    var off = tracked.GetFieldOffset(t.member);
                    if (off == null)
#if DEBUG
                        throw new InvalidCastException("Attempting to subscribe to unknown field");
#else
                        continue;
#endif
                    _trackedObjects.Add((tracked, off.Value));
                }
            }
        }

        StoreOffset = storeOffset;
    }

    public abstract void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, int storeOffset);
    protected abstract ExpressionVariant EvaluateCore(TimeSpan now, ExpressionVariant currentValue);

    public ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue)
    {
        _invalidated = false;
        return EvaluateCore(now, currentValue);
    }

    public virtual void Activate()
    {
        if (_trackedObjects != null)
            foreach (var tracked in _trackedObjects)
                tracked.obj.SubscribeToInvalidation(tracked.member, this);
    }

    public virtual void Deactivate()
    {
        if (_trackedObjects != null)
            foreach (var tracked in _trackedObjects)
                tracked.obj.UnsubscribeFromInvalidation(tracked.member, this);
    }

    public void Invalidate()
    {
        if (_invalidated)
            return;
        _invalidated = true;
        TargetObject.NotifyAnimatedValueChanged(StoreOffset);
    }
}