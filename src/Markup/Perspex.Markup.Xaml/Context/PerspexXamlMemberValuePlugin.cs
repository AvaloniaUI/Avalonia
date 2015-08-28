namespace Perspex.Markup.Xaml.Context
{
    using System;
    using System.Reactive.Linq;
    using Controls;
    using DataBinding;
    using Glass;
    using OmniXaml.Typing;

    public class PerspexXamlMemberValuePlugin : MemberValuePlugin
    {
        private readonly XamlMember xamlMember;
        private readonly IPerspexPropertyBinder propertyBinder;

        public PerspexXamlMemberValuePlugin(XamlMember xamlMember, IPerspexPropertyBinder propertyBinder) : base(xamlMember)
        {
            this.xamlMember = xamlMember;
            this.propertyBinder = propertyBinder;
        }

        public override void SetValue(object instance, object value)
        {
            if (ValueRequiresSpecialHandling(value))
            {
                HandleSpecialValue(instance, value);
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
                HandleXamlBindingDefinition(definition);
            }
            else if (IsPerspexProperty)
            {
                HandlePerspexProperty(instance, value);
            }
            else
            {
                throw new InvalidOperationException($"Cannot handle the value {value} for member {this} and the instance {instance}");
            }
        }

        private void HandlePerspexProperty(object instance, object value)
        {
            var pp = PerspexProperty;
            var po = (PerspexObject) instance;
            po.SetValue(pp, value);
        }

        private void HandleXamlBindingDefinition(XamlBindingDefinition xamlBindingDefinition)
        {
            PerspexObject subjectObject = xamlBindingDefinition.Target;
            propertyBinder.Create(xamlBindingDefinition);

            var observableForDataContext = subjectObject.GetObservable(Control.DataContextProperty);
            observableForDataContext.Where(o => o != null).Subscribe(_ => BindToDataContextWhenItsSet(xamlBindingDefinition));
        }

        private void BindToDataContextWhenItsSet(XamlBindingDefinition definition)
        {
            var target = definition.Target;
            var dataContext = target.DataContext;

            var binding = propertyBinder.GetBinding(target, definition.TargetProperty);
            binding.Bind(dataContext);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public PerspexProperty PerspexProperty
        {
            get
            {
                var underlyingType = xamlMember.DeclaringType.UnderlyingType;
                var name = xamlMember.Name + "Property";

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
            return $"{{Perspex Value Connector for member {xamlMember}}}";
        }
    }
}