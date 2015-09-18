// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Glass;
using OmniXaml.ObjectAssembler;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Markup.Xaml.DataBinding;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlMemberValuePlugin : MemberValuePlugin
    {
        private readonly XamlMember _xamlMember;
        private readonly IPerspexPropertyBinder _propertyBinder;

        public PerspexXamlMemberValuePlugin(XamlMember xamlMember, IPerspexPropertyBinder propertyBinder) : base(xamlMember)
        {
            _xamlMember = xamlMember;
            _propertyBinder = propertyBinder;
        }

        public override void SetValue(object instance, object value)
        {
            if (value is XamlBindingDefinition)
            {
                HandleXamlBindingDefinition((XamlBindingDefinition)value);
            }
            else if (IsPerspexProperty)
            {
                HandlePerspexProperty(instance, value);
            }
            else if (instance is Setter && _xamlMember.Name == "Value")
            {
                var setter = (Setter)instance;
                var targetType = setter.Property.PropertyType;
                var valuePipeline = new ValuePipeline(_xamlMember.TypeRepository, null);
                var xamlType = _xamlMember.TypeRepository.GetXamlType(targetType);
                base.SetValue(instance, valuePipeline.ConvertValueIfNecessary(value, xamlType));
            }
            else
            {
                base.SetValue(instance, value);
            }
        }

        private void HandlePerspexProperty(object instance, object value)
        {
            var pp = PerspexProperty;
            var po = (PerspexObject)instance;
            po.SetValue(pp, value);
        }

        private void HandleXamlBindingDefinition(XamlBindingDefinition xamlBindingDefinition)
        {
            PerspexObject subjectObject = xamlBindingDefinition.Target;
            _propertyBinder.Create(xamlBindingDefinition);

            var observableForDataContext = subjectObject.GetObservable(Control.DataContextProperty);
            observableForDataContext.Where(o => o != null).Subscribe(_ => BindToDataContextWhenItsSet(xamlBindingDefinition));
        }

        private void BindToDataContextWhenItsSet(XamlBindingDefinition definition)
        {
            var target = definition.Target;
            var dataContext = target.DataContext;

            var binding = _propertyBinder.GetBinding(target, definition.TargetProperty);
            binding.BindToDataContext(dataContext);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public PerspexProperty PerspexProperty
        {
            get
            {
                var underlyingType = _xamlMember.DeclaringType.UnderlyingType;
                var name = _xamlMember.Name + "Property";

                var value = ReflectionExtensions.GetValueOfStaticField(underlyingType, name);
                return value as PerspexProperty;
            }
        }

        private bool ValueRequiresSpecialHandling(object value)
        {
            return value is XamlBindingDefinition || IsPerspexProperty;
        }

        private bool IsPerspexProperty => PerspexProperty != null;

        public override string ToString()
        {
            return $"{{Perspex Value Connector for member {_xamlMember}}}";
        }
    }
}