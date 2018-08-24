﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Reactive;

namespace Avalonia.Data.Core
{
    public class AvaloniaPropertyAccessorNode : SettableNode
    {
        private IDisposable _subscription;
        private readonly bool _enableValidation;
        private readonly AvaloniaProperty _property;

        public AvaloniaPropertyAccessorNode(AvaloniaProperty property, bool enableValidation)
        {
            _property = property;
            _enableValidation = enableValidation;
        }

        public override string Description => PropertyName;
        public string PropertyName { get; }
        public override Type PropertyType => _property.PropertyType;

        protected override bool SetTargetValueCore(object value, BindingPriority priority)
        {
            try
            {
                if (Target.IsAlive && Target.Target is IAvaloniaObject obj)
                {
                    obj.SetValue(_property, value, priority);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected override void StartListeningCore(WeakReference reference)
        {
            if (reference.Target is IAvaloniaObject obj)
            {
                _subscription = new AvaloniaPropertyObservable<object>(obj, _property).Subscribe(ValueChanged);
            }
            else
            {
                _subscription = null;
            }
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
