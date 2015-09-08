// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Glass;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Markup.Xaml.DataBinding;

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
            if (this.ValueRequiresSpecialHandling(value))
            {
                this.HandleSpecialValue(instance, value);
            }
            else
            {
                base.SetValue(instance, value);
            }
        }

        private void HandleSpecialValue(object instance, object value)
        {
            var definition = value as XamlBindingDefinition;
            if (definition != null)
            {
                this.HandleXamlBindingDefinition(definition);
            }
            else if (this.IsPerspexProperty)
            {
                this.HandlePerspexProperty(instance, value);
            }
            else
            {
                throw new InvalidOperationException($"Cannot handle the value {value} for member {this} and the instance {instance}");
            }
        }

        private void HandlePerspexProperty(object instance, object value)
        {
            var pp = this.PerspexProperty;
            var po = (PerspexObject)instance;
            po.SetValue(pp, value);
        }

        private void HandleXamlBindingDefinition(XamlBindingDefinition xamlBindingDefinition)
        {
            PerspexObject subjectObject = xamlBindingDefinition.Target;
            _propertyBinder.Create(xamlBindingDefinition);

            var observableForDataContext = subjectObject.GetObservable(Control.DataContextProperty);
            observableForDataContext.Where(o => o != null).Subscribe(_ => this.BindToDataContextWhenItsSet(xamlBindingDefinition));
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
            return value is XamlBindingDefinition || this.IsPerspexProperty;
        }

        private bool IsPerspexProperty => this.PerspexProperty != null;

        public override string ToString()
        {
            return $"{{Perspex Value Connector for member {_xamlMember}}}";
        }
    }
}