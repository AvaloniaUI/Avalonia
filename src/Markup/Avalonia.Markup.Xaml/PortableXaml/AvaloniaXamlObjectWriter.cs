using Avalonia.Data;
using Portable.Xaml;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlObjectWriter : XamlObjectWriter
    {
        public static AvaloniaXamlObjectWriter Create(XamlSchemaContext schemaContext, object instance)
        {
            var writerSettings = new XamlObjectWriterSettings();
            var nameScope = new AvaloniaNameScope { Instance = instance };
            writerSettings.ExternalNameScope = nameScope;
            writerSettings.RegisterNamesOnExternalNamescope = true;
            writerSettings.RootObjectInstance = instance;

            return new AvaloniaXamlObjectWriter(schemaContext, writerSettings, nameScope);
        }

        private AvaloniaXamlObjectWriter(
            XamlSchemaContext schemaContext,
            XamlObjectWriterSettings settings,
            AvaloniaNameScope nameScope
            )
            : base(schemaContext, settings)
        {
            _nameScope = nameScope;
        }

        protected override void OnAfterBeginInit(object value)
        {
            base.OnAfterBeginInit(value);
        }

        protected override void OnAfterEndInit(object value)
        {
            base.OnAfterEndInit(value);
        }

        protected override void OnAfterProperties(object value)
        {
            _delayedValuesHelper.EndInit(value);

            base.OnAfterProperties(value);

            //AfterEndInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            HandleEndEdit(value);

            _objects.Pop();
        }

        protected override void OnBeforeProperties(object value)
        {
            //OnAfterBeginInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks

            HandleBeginInit(value);

            _delayedValuesHelper.BeginInit(value);

            base.OnBeforeProperties(value);

            var target = Current;

            var member = _lastStartMember;
            _lastStartMember = null;

            //if (target != null && value != null && member != null &&
            //    member is PropertyXamlMember && !(value is MarkupExtension) &&
            //    member.DeclaringType.ContentProperty?.Name == member.Name &&
            //    !member.IsReadOnly &&
            //    member.Invoker?.UnderlyingSetter != null)
            //{
            //    //set default content before start properties
            //    try
            //    {
            //        if (!OnSetValue(target, member, value))
            //        {
            //            member.Invoker.SetValue(target, value);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new XamlObjectWriterException($"Set value of member '{member}' threw an exception", ex);
            //    }
            //}

            _objects.Push(value);
        }

        private AvaloniaNameScope _nameScope;

        private Stack<object> _objects = new Stack<object>();

        private object Current => _objects.Count > 0 ? _objects.Peek() : null;

        private XamlMember _lastStartMember = null;

        private void HandleBeginInit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.BeginInit();
        }

        private void HandleEndEdit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.EndInit();
        }

        private void HandleFinished()
        {
            if (_nameScope != null && Result != null)
            {
                _nameScope.RegisterOnNameScope(Result);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HandleFinished();
            }

            base.Dispose(disposing);
        }

        public override void WriteStartMember(XamlMember property)
        {
            base.WriteStartMember(property);

            _lastStartMember = property;
        }

        public override void WriteEndMember()
        {
            base.WriteEndMember();

            _lastStartMember = null;
        }

        protected override bool OnSetValue(object target, XamlMember member, object value)
        {
            if (value is IBinding)
            {
                //delay bindings
                _delayedValuesHelper.Add(new DelayedValue(target, member, value));
                return true;
            }

            return base.OnSetValue(target, member, value);
        }

        private readonly DelayedValuesHelper _delayedValuesHelper = new DelayedValuesHelper();

        private class DelayedValue
        {
            public DelayedValue(object target, XamlMember member, object value)
            {
                Target = target;
                Member = member;
                Value = value;
            }

            public object Target { get; }

            public XamlMember Member { get; }

            public object Value { get; }
        }

        private class DelayedValuesHelper
        {
            private HashSet<object> _targets = new HashSet<object>();

            private IList<DelayedValue> _values = new List<DelayedValue>();

            private int cnt;

            public void BeginInit(object target)
            {
                ++cnt;

                AddTargetIfNeeded(target);
            }

            public void EndInit(object target)
            {
                --cnt;

                if (cnt == 0)
                {
                    EndInit();
                }
            }

            private void AddTargetIfNeeded(object target)
            {
                if (!_targets.Contains(target))
                {
                    Add(new DelayedValue(target, null, null));
                }
            }

            public void Add(DelayedValue value)
            {
                _values.Add(value);

                var target = value.Target;

                if (!_targets.Contains(value.Target))
                {
                    _targets.Add(target);
                    (target as ISupportInitialize)?.BeginInit();
                }
            }

            private void EndInit()
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

            private IEnumerable<DelayedValue> Values => _values;
        }
    }
}