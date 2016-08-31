// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Validates properties on objects that implement <see cref="INotifyDataErrorInfo"/>.
    /// </summary>
    public class IndeiValidationPlugin : IDataValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference reference, string memberName) => reference.Target is INotifyDataErrorInfo;

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor accessor)
        {
            return new Validator(reference, name, accessor);
        }

        private class Validator : DataValidatiorBase, IWeakSubscriber<DataErrorsChangedEventArgs>
        {
            WeakReference _reference;
            string _name;

            public Validator(WeakReference reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                _reference = reference;
                _name = name;
            }

            void IWeakSubscriber<DataErrorsChangedEventArgs>.OnEvent(object sender, DataErrorsChangedEventArgs e)
            {
                if (e.PropertyName == _name || string.IsNullOrEmpty(e.PropertyName))
                {
                    Observer.OnNext(CreateBindingNotification(Value));
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                var target = _reference.Target as INotifyDataErrorInfo;

                if (target != null)
                {
                    WeakSubscriptionManager.Unsubscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }
            }

            protected override void SubscribeCore(IObserver<object> observer)
            {
                var target = _reference.Target as INotifyDataErrorInfo;

                if (target != null)
                {
                    WeakSubscriptionManager.Subscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }

                base.SubscribeCore(observer);
            }

            protected override void InnerValueChanged(object value)
            {
                base.InnerValueChanged(CreateBindingNotification(value));
            }

            private BindingNotification CreateBindingNotification(object value)
            {
                var target = (INotifyDataErrorInfo)_reference.Target;

                if (target != null)
                {
                    var errors = target.GetErrors(_name)?
                        .Cast<String>()
                        .Where(x => x != null).ToList();

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

            private Exception GenerateException(IList<string> errors)
            {
                if (errors.Count == 1)
                {
                    return new Exception(errors[0]);
                }
                else
                {
                    return new AggregateException(
                        errors.Select(x => new Exception(x)));
                }
            }
        }
    }
}
