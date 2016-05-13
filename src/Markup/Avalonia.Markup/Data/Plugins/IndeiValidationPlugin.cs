// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Validates properties on objects that implement <see cref="INotifyDataErrorInfo"/>.
    /// </summary>
    public class IndeiValidationPlugin : IValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference reference)
        {
            return reference.Target is INotifyDataErrorInfo;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback)
        {
            return new IndeiValidationChecker(reference, name, accessor, callback);
        }

        private class IndeiValidationChecker : ValidatingPropertyAccessorBase, IWeakSubscriber<DataErrorsChangedEventArgs>
        {
            public IndeiValidationChecker(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback)
                : base(reference, name, accessor, callback)
            {
                var target = reference.Target as INotifyDataErrorInfo;
                if (target != null)
                {
                    if (target.HasErrors)
                    {
                        SendValidationCallback(new IndeiValidationStatus(target.GetErrors(name)));
                    }
                    WeakSubscriptionManager.Subscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                var target = _reference.Target as INotifyDataErrorInfo;
                if (target != null)
                {
                    WeakSubscriptionManager.Unsubscribe(
                        target,
                        nameof(target.ErrorsChanged),
                        this);
                }
            }

            public void OnEvent(object sender, DataErrorsChangedEventArgs e)
            {
                if (e.PropertyName == _name || string.IsNullOrEmpty(e.PropertyName))
                {
                    var indei = _reference.Target as INotifyDataErrorInfo;
                    SendValidationCallback(new IndeiValidationStatus(indei.GetErrors(e.PropertyName)));
                }
            }
        }

        /// <summary>
        /// Describes the current validation status of a property as reported by an object that implements <see cref="INotifyDataErrorInfo"/>.
        /// </summary>
        public class IndeiValidationStatus : IValidationStatus
        {
            internal IndeiValidationStatus(IEnumerable errors)
            {
                Errors = errors;
            }

            /// <inheritdoc/>
            public bool IsValid => !Errors?.OfType<object>().Any() ?? true;

            /// <summary>
            /// The errors on the given property and on the object as a whole.
            /// </summary>
            public IEnumerable Errors { get; }
        }
    }
}
