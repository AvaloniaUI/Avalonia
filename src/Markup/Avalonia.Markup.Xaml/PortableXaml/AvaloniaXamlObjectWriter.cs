using Avalonia.Data;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlObjectWriter : XamlObjectWriter
    {
        public static AvaloniaXamlObjectWriter Create(
            XamlSchemaContext schemaContext,
            AvaloniaXamlContext context)
        {
            var nameScope = new AvaloniaNameScope { Instance = context?.RootInstance };

            var writerSettings = new XamlObjectWriterSettings()
            {
                ExternalNameScope = nameScope,
                RegisterNamesOnExternalNamescope = true,
                RootObjectInstance = context?.RootInstance
            };

            return new AvaloniaXamlObjectWriter(schemaContext,
                                                writerSettings.WithContext(context),
                                                nameScope);
        }

        private readonly DelayedValuesHelper _delayedValuesHelper = new DelayedValuesHelper();

        private AvaloniaNameScope _nameScope;

        private AvaloniaXamlObjectWriter(
            XamlSchemaContext schemaContext,
            XamlObjectWriterSettings settings,
            AvaloniaNameScope nameScope
            )
            : base(schemaContext, settings)
        {
            _nameScope = nameScope;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_nameScope != null && Result != null)
                {
                    _nameScope.RegisterOnNameScope(Result);
                }
            }

            base.Dispose(disposing);
        }

        public void ApplyAllDelayedProperties()
        {
            //HACK: We need this because Begin/EndInit ordering is broken
            _delayedValuesHelper.ApplyAll();
        }

        protected internal override void OnAfterBeginInit(object value)
        {
            //not called for avalonia objects
            //as it's called inly for
            //Portable.Xaml.ComponentModel.ISupportInitialize
            base.OnAfterBeginInit(value);
        }

        protected internal override void OnAfterEndInit(object value)
        {
            //not called for avalonia objects
            //as it's called inly for
            //Portable.Xaml.ComponentModel.ISupportInitialize
            base.OnAfterEndInit(value);
        }

        protected internal override void OnAfterProperties(object value)
        {
            _delayedValuesHelper.EndInit(value);

            base.OnAfterProperties(value);

            //AfterEndInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            HandleEndEdit(value);
        }

        protected internal override void OnBeforeProperties(object value)
        {
            //OnAfterBeginInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            HandleBeginInit(value);
            if (value != null)
                _delayedValuesHelper.BeginInit(value);

            base.OnBeforeProperties(value);
        }

        protected internal override bool OnSetValue(object target, XamlMember member, object value)
        {
            if (_delayedValuesHelper.TryAdd(target, member, value))
            {
                return true;
            }

            return base.OnSetValue(target, member, value);
        }

        private void HandleBeginInit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.BeginInit();
        }

        private void HandleEndEdit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.EndInit();
        }

        private class DelayedValuesHelper
        {
            private int _cnt;

            private HashSet<object> _targets = new HashSet<object>();

            private IList<DelayedValue> _values = new List<DelayedValue>();

            private IEnumerable<DelayedValue> Values => _values;

            public void BeginInit(object target)
            {
                ++_cnt;

                AddTargetIfNeeded(target);
            }

            public void EndInit(object target)
            {
                --_cnt;

                if (_cnt == 0)
                {
                    ApplyAll();
                }
            }

            public bool TryAdd(object target, XamlMember member, object value)
            {
                if (value is IBinding)
                {
                    Add(new DelayedValue(target, member, value));

                    return true;
                }

                return false;
            }

            private void Add(DelayedValue value)
            {
                _values.Add(value);

                var target = value.Target;

                if (!_targets.Contains(value.Target))
                {
                    _targets.Add(target);
                    (target as ISupportInitialize)?.BeginInit();
                }
            }

            private void AddTargetIfNeeded(object target)
            {
                if (!_targets.Contains(target))
                {
                    Add(new DelayedValue(target, null, null));
                }
            }

            public void ApplyAll()
            {
                //TODO: revisit this
                //apply delayed values and clear
                //that's the last object let's set all delayed bindings
                foreach (var dv in Values.Where(v => v.Member != null))
                {
                    dv.Member.Invoker.SetValue(dv.Target, dv.Value);
                }

                //TODO: check/add some order of end init
                //currently we are sending end init in the order of
                //objects creation
                foreach (var v in Values)
                {
                    var target = v.Target;

                    if (_targets.Contains(target))
                    {
                        _targets.Remove(target);
                        (target as ISupportInitialize)?.EndInit();
                    }
                }

                _targets.Clear();
                _values.Clear();
            }

            private class DelayedValue
            {
                public DelayedValue(object target, XamlMember member, object value)
                {
                    Target = target;
                    Member = member;
                    Value = value;
                }

                public XamlMember Member { get; }
                public object Target { get; }
                public object Value { get; }
            }
        }
    }
}