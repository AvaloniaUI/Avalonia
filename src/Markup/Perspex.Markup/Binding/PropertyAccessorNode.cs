// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reflection;

namespace Perspex.Markup.Binding
{
    public class PropertyAccessorNode : ExpressionNode
    {
        private PropertyInfo _propertyInfo;

        public PropertyAccessorNode(string propertyName)
        {
            PropertyName = propertyName;
        }

        public bool SetValue(object value)
        {
            if (_propertyInfo != null)
            {
                _propertyInfo.SetValue(Target, value);
                return true;
            }

            return false;
        }

        public string PropertyName { get; }

        protected override void SubscribeAndUpdate(object target)
        {
            var result = ExpressionValue.None;

            if (target != null)
            {
                _propertyInfo = target.GetType().GetTypeInfo().GetDeclaredProperty(PropertyName);

                if (_propertyInfo != null)
                {
                    result = new ExpressionValue(_propertyInfo.GetValue(target));

                    var inpc = target as INotifyPropertyChanged;

                    if (inpc != null)
                    {
                        inpc.PropertyChanged += PropertyChanged;
                    }
                }
            }
            else
            {
                _propertyInfo = null;
            }

            CurrentValue = result;
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyName)
            {
                CurrentValue = new ExpressionValue(_propertyInfo.GetValue(Target));
            }
        }

        protected override void Unsubscribe(object target)
        {
            var inpc = target as INotifyPropertyChanged;

            if (inpc != null)
            {
                inpc.PropertyChanged -= PropertyChanged;
            }
        }
    }
}
