using System;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    public sealed class TransformGroup : Transform
    {
        /// <summary>
        /// Defines the <see cref="Children"/> property.
        /// </summary>
        public static readonly StyledProperty<Transforms> ChildrenProperty =
            AvaloniaProperty.Register<TransformGroup, Transforms>(nameof(Children));

        private IDisposable? _childrenNotificationSubscription;
        private readonly EventHandler _childTransformChangedHandler;
        private Matrix? _lastMatrix;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public TransformGroup()
        {
            _childTransformChangedHandler = (_, _) =>
            {
                _lastMatrix = null;
                RaiseChanged();
            };
            Children = new Transforms();
        }

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        [Content]
        public Transforms Children
        {
            get { return GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix" />.
        /// </summary>
        public override Matrix Value
        {
            get
            {
                if (_lastMatrix is null)
                {
                    var matrix = Matrix.Identity;
                    foreach (var t in Children)
                    {
                        matrix *= t.Value;
                    }
                    _lastMatrix = matrix;
                }
                return _lastMatrix.Value;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ChildrenProperty)
            {
                _childrenNotificationSubscription?.Dispose();
                if (change.OldValue is Transforms oldTransforms)
                {
                    foreach (var item in oldTransforms)
                    {
                        item.Changed -= _childTransformChangedHandler;
                    }
                }
                if (change.NewValue is Transforms newTransforms)
                {
                    // Ensure reset behavior is Remove
                    newTransforms.ResetBehavior = ResetBehavior.Remove;
                    _childrenNotificationSubscription = newTransforms.ForEachItem(
                        (tr) => tr.Changed += _childTransformChangedHandler,
                        (tr) => tr.Changed -= _childTransformChangedHandler,
                        () => { });
                }
            }
        }
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
    }
}
