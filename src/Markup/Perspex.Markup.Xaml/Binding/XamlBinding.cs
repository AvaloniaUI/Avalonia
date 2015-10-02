// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OmniXaml.TypeConversion;
using Perspex.Controls;
using Perspex.Markup.Binding;

namespace Perspex.Markup.Xaml.Binding
{
    public class XamlBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        public XamlBinding()
        {
        }

        public XamlBinding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        public IObservablePropertyBag Target { get; set; }

        public PerspexProperty TargetProperty { get; set; }

        public string SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void Bind()
        {
            Bind(new ExpressionSubject(CreateExpressionObserver()));
        }

        public ExpressionObserver CreateExpressionObserver()
        {
            var result = new ExpressionObserver(null, SourcePropertyPath);
            var dataContext = Target.GetObservable(Control.DataContextProperty);
            dataContext.Subscribe(x => result.Root = x);
            return result;
        }

        internal void Bind(ISubject<object> subject)
        {
            var mode = BindingMode == BindingMode.Default ?
                TargetProperty.DefaultBindingMode : BindingMode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    Target.Bind(TargetProperty, subject);
                    break;
                case BindingMode.TwoWay:
                    Target.BindTwoWay(TargetProperty, subject);
                    break;
                case BindingMode.OneTime:
                    Target.GetObservable(Control.DataContextProperty).Subscribe(dataContext =>
                    {
                        subject.Take(1).Subscribe(x => Target.SetValue(TargetProperty, x));
                    });                    
                    break;
                case BindingMode.OneWayToSource:
                    Target.GetObservable(TargetProperty).Subscribe(subject);
                    break;
            }
        }
    }
}