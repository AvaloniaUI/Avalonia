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

        public string SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = new ExpressionSubject(
                CreateExpressionObserver(instance, property),
                property.PropertyType);

            if (subject != null)
            {
                Bind(instance, property, subject);
            }
        }

        public ExpressionObserver CreateExpressionObserver(
            IObservablePropertyBag instance, 
            PerspexProperty property)
        {
            IObservable<object> dataContext = null;

            if (property != Control.DataContextProperty)
            {
                dataContext = instance.GetObservable(Control.DataContextProperty);
            }
            else
            {
                var parent = instance.InheritanceParent as IObservablePropertyBag;

                if (parent != null)
                {
                    dataContext = parent.GetObservable(Control.DataContextProperty);
                }
            }

            if (dataContext != null)
            {
                var result = new ExpressionObserver(null, SourcePropertyPath);
                dataContext.Subscribe(x => result.Root = x);
                return result;
            }

            return null;
        }

        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            var mode = BindingMode == BindingMode.Default ?
                property.DefaultBindingMode : BindingMode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    target.Bind(property, subject);
                    break;
                case BindingMode.TwoWay:
                    target.BindTwoWay(property, subject);
                    break;
                case BindingMode.OneTime:
                    target.GetObservable(Control.DataContextProperty).Subscribe(dataContext =>
                    {
                        subject.Take(1).Subscribe(x => target.SetValue(property, x));
                    });                    
                    break;
                case BindingMode.OneWayToSource:
                    target.GetObservable(property).Subscribe(subject);
                    break;
            }
        }
    }
}