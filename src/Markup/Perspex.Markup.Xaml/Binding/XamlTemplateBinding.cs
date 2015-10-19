// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using OmniXaml.TypeConversion;
using Perspex.Controls;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Binding
{
    public class XamlTemplateBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        public XamlTemplateBinding()
        {
        }

        public XamlTemplateBinding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        public string SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void Bind(PerspexObject instance, PerspexProperty targetProperty)
        {
            instance.GetObservable(Control.TemplatedParentProperty)
                .Where(x => x != null)
                .OfType<PerspexObject>()
                .Take(1)
                .Subscribe(x => BindToTemplatedParent(instance, targetProperty, x));
        }

        private void BindToTemplatedParent(
            PerspexObject instance, 
            PerspexProperty targetProperty,
            PerspexObject templatedParent)
        {
            var sourceProperty = PerspexPropertyRegistry.Instance.FindRegistered(instance.GetType(), SourcePropertyPath);

            if (sourceProperty == null)
            {
                throw new InvalidOperationException(
                    $"The property {SourcePropertyPath} could not be found on {templatedParent.GetType()}.");
            }

            instance.Bind(targetProperty, templatedParent.GetObservable(sourceProperty), BindingPriority.Style);
        }
    }
}