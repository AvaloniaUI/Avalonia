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

        public BindingMode Mode { get; set; }
        public BindingPriority Priority { get; set; }
        public RelativeSource RelativeSource { get; set; }
        public string SourcePropertyPath { get; set; }

        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = new ExpressionSubject(
                CreateExpressionObserver(instance, property),
                property.PropertyType);

            if (subject != null)
            {
                if (RelativeSource?.Mode != RelativeSourceMode.TemplatedParent)
                {
                    Bind(instance, property, subject);
                }
                else
                {
                    instance.GetObservable(Control.TemplatedParentProperty)
                        .Where(x => x != null)
                        .OfType<PerspexObject>()
                        .Take(1)
                        .Subscribe(x => BindToTemplatedParent((PerspexObject)instance, property, x));
                }
            }
        }

        public ExpressionObserver CreateExpressionObserver(
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

        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            var mode = Mode == BindingMode.Default ?
                property.DefaultBindingMode : Mode;

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

        private void BindToTemplatedParent(
            PerspexObject instance,
            PerspexProperty targetProperty,
            PerspexObject templatedParent)
        {
            var sourceProperty = PerspexPropertyRegistry.Instance.FindRegistered(
                templatedParent,
                SourcePropertyPath);

            if (sourceProperty == null)
            {
                throw new InvalidOperationException(
                    $"The property {SourcePropertyPath} could not be found on {templatedParent.GetType()}.");
            }

            instance.Bind(targetProperty, templatedParent.GetObservable(sourceProperty), BindingPriority.Style);
            templatedParent.Bind(sourceProperty, instance.GetObservable(targetProperty), BindingPriority.Style);
        }

    }
}