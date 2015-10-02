// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OmniXaml.TypeConversion;
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

        public object Source { get; set; }

        public string SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void Bind()
        {
            Bind(new ExpressionSubject(CreateExpressionObserver()));
        }

        public ExpressionObserver CreateExpressionObserver()
        {
            var path = SourcePropertyPath;
            var source = Source;

            if (source == null)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    path = "DataContext." + path;
                }

                source = Target;
            }

            return new ExpressionObserver(source, path);
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
                    throw new NotImplementedException();
                case BindingMode.OneWayToSource:
                    Target.GetObservable(TargetProperty).Subscribe(subject);
                    break;
            }
        }
    }
}