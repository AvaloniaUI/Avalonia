using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties on objects that implement <see cref="INotifyDataErrorInfo"/>.
    /// </summary>
    public class IndeiValidationPlugin : IDataValidationPlugin
    {
        private static readonly WeakEvent<INotifyDataErrorInfo, DataErrorsChangedEventArgs>
            ErrorsChangedWeakEvent = WeakEvent.Register<INotifyDataErrorInfo, DataErrorsChangedEventArgs>(
                (s, h) => s.ErrorsChanged += h,
                (s, h) => s.ErrorsChanged -= h
            );

        /// <inheritdoc/>
        public bool Match(WeakReference<object?> reference, string memberName)
        {
            reference.TryGetTarget(out var target);

            return target is INotifyDataErrorInfo;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference<object?> reference, string name, IPropertyAccessor accessor)
        {
            return new Validator(reference, name, accessor);
        }

        private class Validator : DataValidationBase, IWeakEventSubscriber<DataErrorsChangedEventArgs>
        {
            private readonly WeakReference<object?> _reference;
            private readonly string _name;

            public Validator(WeakReference<object?> reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                _reference = reference;
                _name = name;
            }

            void IWeakEventSubscriber<DataErrorsChangedEventArgs>.OnEvent(object? notifyDataErrorInfo, WeakEvent ev, DataErrorsChangedEventArgs e)
            {
                if (e.PropertyName == _name || string.IsNullOrEmpty(e.PropertyName))
                {
                    PublishValue(CreateBindingNotification(Value));
                }
            }

            protected override void SubscribeCore()
            {
                var target = GetReferenceTarget() as INotifyDataErrorInfo;

                if (target != null)
                {
                    ErrorsChangedWeakEvent.Subscribe(target, this);
                }

                base.SubscribeCore();
            }

            protected override void UnsubscribeCore()
            {
                var target = GetReferenceTarget() as INotifyDataErrorInfo;

                if (target != null)
                {
                    ErrorsChangedWeakEvent.Unsubscribe(target, this);
                }

                base.UnsubscribeCore();
            }

            protected override void InnerValueChanged(object? value)
            {
                PublishValue(CreateBindingNotification(value));
            }

            private BindingNotification CreateBindingNotification(object? value)
            {
                if (GetReferenceTarget() is INotifyDataErrorInfo target)
                {
                    var errors = target.GetErrors(_name)?
                        .Cast<object>()
                        .Where(x => x != null)
                        .ToList();

                    if (errors?.Count > 0)
                    {
                        return new BindingNotification(
                            GenerateException(errors),
                            BindingErrorType.DataValidationError,
                            value);
                    }
                }

                return new BindingNotification(value);
            }

            private object? GetReferenceTarget()
            {
                _reference.TryGetTarget(out var target);

                return target;
            }

            private static Exception GenerateException(IList<object> errors)
            {
                if (errors.Count == 1)
                {
                    return new DataValidationException(errors[0]);
                }
                else
                {
                    return new AggregateException(
                        errors.Select(x => new DataValidationException(x)));
                }
            }
        }
    }
}
