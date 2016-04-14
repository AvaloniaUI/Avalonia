using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Data;
using System.ComponentModel;
using System.Collections;
using Perspex.Utilities;

namespace Perspex.Markup.Data.Plugins
{
    class IndeiValidationCheckerPlugin : IValidationCheckerPlugin
    {
        public ValidationCheckerBase Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback)
        {
            return new IndeiValidationChecker(reference, name, accessor, callback);
        }

        private class IndeiValidationChecker : ValidationCheckerBase, IWeakSubscriber<DataErrorsChangedEventArgs>
        {
            public IndeiValidationChecker(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback)
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

        private class IndeiValidationStatus : ValidationStatus
        {
            public IndeiValidationStatus(IEnumerable errors)
            {
                Errors = errors;
            }
            public override bool IsValid => !Errors.OfType<object>().Any();

            public IEnumerable Errors { get; }
        }
    }
}
