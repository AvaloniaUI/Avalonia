using System;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance : ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly AvaloniaProperty _property;
        private readonly BindingPriority _priority;
        private readonly InstancedBinding _binding;
        private IDisposable? _subscription;
        private bool _isActive;

        public PropertySetterBindingInstance(
            IStyleable target,
            AvaloniaProperty property,
            BindingPriority priority,
            IBinding binding)
        {
            _target = target;
            _property = property;
            _priority = priority;
            _binding = binding.Initiate(target, property).WithPriority(priority);
        }

        public void Activate()
        {
            if (!_isActive)
            {
                _subscription = BindingOperations.Apply(_target, _property, _binding, null);
                _isActive = true;
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                _subscription?.Dispose();
                _subscription = null;
                _isActive = false;
            }
        }
    }
}
