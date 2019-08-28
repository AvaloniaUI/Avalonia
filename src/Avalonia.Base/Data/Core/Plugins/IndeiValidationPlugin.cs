// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        /// <inheritdoc/>
        public bool Match(WeakReference<object> reference, string memberName)
        {
            reference.TryGetTarget(out object target);

            return target is INotifyDataErrorInfo;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference<object> reference, string name, IPropertyAccessor accessor)
        {
            return new Validator(reference, name, accessor);
        }

        private class Validator : DataValidationBase, IWeakSubscriber<DataErrorsChangedEventArgs>
        {
            private readonly WeakReference<object> _reference;
            private readonly string _name;

            public Validator(WeakReference<object> reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                _reference = reference;
                _name = name;
            }

            void IWeakSubscriber<DataErrorsChangedEventArgs>.OnEvent(object sender, DataErrorsChangedEventArgs e)
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
                    WeakSubscriptionManager.Subscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }

                base.SubscribeCore();
            }

            protected override void UnsubscribeCore()
            {
                var target = GetReferenceTarget() as INotifyDataErrorInfo;

                if (target != null)
                {
                    WeakSubscriptionManager.Unsubscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }

                base.UnsubscribeCore();
            }

            protected override void InnerValueChanged(object value)
            {
                PublishValue(CreateBindingNotification(value));
            }

            private BindingNotification CreateBindingNotification(object value)
            {
                var target = (INotifyDataErrorInfo)GetReferenceTarget();

                if (target != null)
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

            private object GetReferenceTarget()
            {
                _reference.TryGetTarget(out object target);

                return target;
            }

            private Exception GenerateException(IList<object> errors)
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
