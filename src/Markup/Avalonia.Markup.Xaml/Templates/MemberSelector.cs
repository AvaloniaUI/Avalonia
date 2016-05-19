// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Data;
using System;

namespace Avalonia.Markup.Xaml.Templates
{
    public class MemberSelector : IMemberSelector
    {
        public string MemberName
        {
            get { return _memberName; }
            set
            {
                if (_memberName != value)
                {
                    _memberName = value;
                    _expressionNode = null;
                    _memberValueNode = null;
                }
            }
        }

        public object Select(object o)
        {
            if (string.IsNullOrEmpty(MemberName))
            {
                return o;
            }

            if (_expressionNode == null)
            {
                _expressionNode = ExpressionNodeBuilder.Build(MemberName);

                _memberValueNode = _expressionNode;

                while (_memberValueNode.Next != null)
                    _memberValueNode = _memberValueNode.Next;
            }

            _expressionNode.Target = new WeakReference(o);

            object result = _memberValueNode.CurrentValue.Target;

            if (result == AvaloniaProperty.UnsetValue)
            {
                return null;
            }
            else if (result is BindingError)
            {
                return null;
            }

            return result;
        }

        private ExpressionNode _expressionNode;
        private string _memberName;
        private ExpressionNode _memberValueNode;
    }
}