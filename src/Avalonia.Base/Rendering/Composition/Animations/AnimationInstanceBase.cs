using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations;

/// <summary>
/// The base class for both key-frame and expression animation instances
/// Is responsible for activation tracking and for subscribing to properties used in dependencies
/// </summary>
internal abstract class AnimationInstanceBase : IAnimationInstance
{
    private List<(ServerObject obj, CompositionProperty member)>? _trackedObjects;
    protected PropertySetSnapshot Parameters { get; }
    public ServerObject TargetObject { get; }
    protected CompositionProperty Property { get; private set; } = null!;
    private bool _invalidated;

    public AnimationInstanceBase(ServerObject target, PropertySetSnapshot parameters)
    {
        Parameters = parameters;
        TargetObject = target;
    }

    protected void Initialize(CompositionProperty property, HashSet<(string name, string member)> trackedObjects)
    {
        if (trackedObjects.Count > 0)
        {
            _trackedObjects = new ();
            foreach (var t in trackedObjects)
            {
                var obj = Parameters.GetObjectParameter(t.name);
                if (obj is ServerObject tracked)
                {
                    var off = tracked.GetCompositionProperty(t.member);
                    if (off == null)
#if DEBUG
                        throw new InvalidCastException("Attempting to subscribe to unknown field");
#else
                        continue;
#endif
                    _trackedObjects.Add((tracked, off));
                }
            }
        }

        Property = property;
    }

    public abstract void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property);
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
        TargetObject.NotifyAnimatedValueChanged(Property);
    }

    public void OnTick() => Invalidate();
}
