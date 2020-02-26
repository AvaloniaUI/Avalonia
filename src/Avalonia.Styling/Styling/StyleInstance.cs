using System;
using System.Collections.Generic;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    internal class StyleInstance : IStyleInstance, IStyleActivatorSink
    {
        private readonly List<ISetterInstance> _setters;
        private readonly IStyleActivator? _activator;
        private bool _active;

        public StyleInstance(
            IStyle source,
            IStyleable target,
            IReadOnlyList<ISetter> setters,
            IStyleActivator? activator = null)
        {
            setters = setters ?? throw new ArgumentNullException(nameof(setters));

            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));

            _setters = new List<ISetterInstance>(setters.Count);
            _activator = activator;

            foreach (var setter in setters)
            {
                _setters.Add(setter.Instance(target, activator is object));
            }
        }

        public IStyle Source { get; }
        public IStyleable Target { get; }

        public void Start()
        {
            if (_activator == null)
            {
                ActivatorChanged(true);
            }
            else
            {
                _activator.Subscribe(this, 0);
            }
        }

        public void Dispose()
        {
            ActivatorChanged(false);
            _activator?.Dispose();
        }

        private void ActivatorChanged(bool value)
        {
            if (_active != value)
            {
                _active = value;

                if (_active)
                {
                    foreach (var setter in _setters)
                    {
                        setter.Activate();
                    }
                }
                else
                {
                    foreach (var setter in _setters)
                    {
                        setter.Deactivate();
                    }
                }
            }
        }

        void IStyleActivatorSink.OnNext(bool value, int tag) => ActivatorChanged(value);
    }
}
