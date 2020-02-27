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

            var setterCount = setters.Count;

            _setters = new List<ISetterInstance>(setterCount);
            _activator = activator;

            for (var i = 0; i < setterCount; ++i)
            {
                _setters.Add(setters[i].Instance(Target));
            }
        }

        public IStyle Source { get; }
        public IStyleable Target { get; }

        public void Start()
        {
            var hasActivator = _activator is object;

            foreach (var setter in _setters)
            {
                setter.Start(hasActivator);
            }

            if (hasActivator)
            {
                _activator!.Subscribe(this, 0);
            }
        }

        public void Dispose()
        {
            foreach (var setter in _setters)
            {
                setter.Dispose();
            }

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
