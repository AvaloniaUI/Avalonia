// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Data;
using System;
using System.Reactive.Linq;

namespace Avalonia.Markup.Xaml.Templates
{
    public class MemberSelector : IMemberSelector
    {
        private string _memberName;

        public string MemberName
        {
            get { return _memberName; }
            set
            {
                if (_memberName != value)
                {
                    _memberName = value;
                }
            }
        }

        public object Select(object o)
        {
            if (string.IsNullOrEmpty(MemberName))
            {
                return o;
            }

            var expression = new ExpressionObserver(o, MemberName);
            object result = AvaloniaProperty.UnsetValue;

            expression.Subscribe(x => result = x);
            return (result == AvaloniaProperty.UnsetValue || result is BindingNotification) ? null : result;
        }
    }
}