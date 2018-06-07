using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Avalonia.Data.Core
{
    public class AvaloniaPropertyAccessorNode : ExpressionNode, ISettableNode
    {
        private readonly bool _enableValidation;
        private readonly AvaloniaProperty _property;

        public AvaloniaPropertyAccessorNode(AvaloniaProperty property, bool enableValidation)
        {
            _property = property;
            _enableValidation = enableValidation;
        }

        public override string Description => PropertyName;
        public string PropertyName { get; }
        public Type PropertyType => _property.PropertyType;

        public bool SetTargetValue(object value, BindingPriority priority)
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

        protected override IObservable<object> StartListeningCore(WeakReference reference)
        {
            return (reference.Target as IAvaloniaObject)?.GetWeakObservable(_property) ?? Observable.Empty<object>();
        }
    }
}
