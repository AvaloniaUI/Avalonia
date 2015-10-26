// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OmniXaml.TypeConversion;
using Perspex.Controls;
using Perspex.Markup.Data;

namespace Perspex.Markup.Xaml.Data
{
    public class Binding : IBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        public Binding()
        {
        }

        public Binding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        public IValueConverter Converter { get; set; }
        public BindingMode Mode { get; set; }
        public BindingPriority Priority { get; set; }
        public RelativeSource RelativeSource { get; set; }
        public string SourcePropertyPath { get; set; }

        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = CreateExpressionSubject(instance, property);

            if (subject != null)
            {
                Bind(instance, property, subject);
            }
        }

        public ISubject<object> CreateExpressionSubject(
            IObservablePropertyBag instance, 
            PerspexProperty property)
        {
            ExpressionObserver observer;

            if (RelativeSource == null || RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContextExpressionSubject(instance, property);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentExpressionSubject(instance, property);
            }
            else
            {
                throw new NotSupportedException();
            }

            return new ExpressionSubject(
                observer, 
                property.PropertyType, 
                Converter ?? DefaultValueConverter.Instance);
        }

        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            var mode = Mode == BindingMode.Default ?
                property.DefaultBindingMode : Mode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    target.Bind(property, subject, Priority);
                    break;
                case BindingMode.TwoWay:
                    target.BindTwoWay(property, subject, Priority);
                    break;
                case BindingMode.OneTime:
                    target.GetObservable(Control.DataContextProperty).Subscribe(dataContext =>
                    {
                        subject.Take(1).Subscribe(x => target.SetValue(property, x, Priority));
                    });                    
                    break;
                case BindingMode.OneWayToSource:
                    target.GetObservable(property).Subscribe(subject);
                    break;
            }
        }

        public ExpressionObserver CreateDataContextExpressionSubject(
            IObservablePropertyBag instance,
            PerspexProperty property)
        {
            var dataContextHost = property != Control.DataContextProperty ?
                instance :
                instance.InheritanceParent as IObservablePropertyBag;

            if (dataContextHost != null)
            {
                var result = new ExpressionObserver(
                    () => dataContextHost.GetValue(Control.DataContextProperty),
                    SourcePropertyPath);
                dataContextHost.GetObservable(Control.DataContextProperty).Subscribe(x =>
                    result.UpdateRoot());
                return result;
            }

            return null;
        }

        public ExpressionObserver CreateTemplatedParentExpressionSubject(
            IObservablePropertyBag instance,
            PerspexProperty property)
        {
            var result = new ExpressionObserver(
                () => instance.GetValue(Control.TemplatedParentProperty),
                SourcePropertyPath);

            if (instance.GetValue(Control.TemplatedParentProperty) == null)
            {
                // TemplatedParent should only be set once, so only listen for the first non-null
                // value.
                instance.GetObservable(Control.TemplatedParentProperty)
                    .Where(x => x != null)
                    .Take(1)
                    .Subscribe(x => result.UpdateRoot());
            }

            return result;
        }
    }
}