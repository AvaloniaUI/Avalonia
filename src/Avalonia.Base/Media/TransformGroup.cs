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

        private IDisposable? _childrenNofiticationSubscription = default;
        private readonly EventHandler ChildTransform_Changed_Handler;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public TransformGroup()
        {
            ChildTransform_Changed_Handler = (_, _) => RaiseChanged();
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
                Matrix result = Matrix.Identity;

                foreach (var t in Children)
                {
                    result *= t.Value;
                }

                return result;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ChildrenProperty)
            {
                _childrenNofiticationSubscription?.Dispose();
                if (change.OldValue is Transforms oldTransforms)
                {
                    foreach (var item in oldTransforms)
                    {
                        item.Changed -= ChildTransform_Changed_Handler;
                    }
                }
                if (change.NewValue is Transforms newTransforms)
                {
                    // Ensure reset behavior is Remove
                    newTransforms.ResetBehavior = ResetBehavior.Remove;
                    _childrenNofiticationSubscription = newTransforms.ForEachItem(
                        (tr) => tr.Changed += ChildTransform_Changed_Handler,
                        (tr) => tr.Changed -= ChildTransform_Changed_Handler,
                        () => { });
                }
            }
        }
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
    }
}
