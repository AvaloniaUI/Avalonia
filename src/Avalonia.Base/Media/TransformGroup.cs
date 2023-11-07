using System.ComponentModel;
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

        private readonly PropertyChangedEventHandler _childrenChangedHandler;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public TransformGroup()
        {
            _childrenChangedHandler = ChildTransform_Changed;
            Children = new Transforms();
        }

        private void ChildTransform_Changed(object? sender, System.EventArgs e)
        {
            this.RaiseChanged();
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
                if (change.OldValue is Transforms oldTransforms)
                {
                    oldTransforms.PropertyChanged -= _childrenChangedHandler;
                }
                if (change.NewValue is Transforms newTransforms)
                {
                    // Ensure reset behavior is Remove
                    newTransforms.ResetBehavior = ResetBehavior.Remove;
                    newTransforms.PropertyChanged += _childrenChangedHandler;
                }
            }
        }
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
        private static readonly PropertyChangedEventArgs IndexPropertyChanged =
            new PropertyChangedEventArgs(string.Empty);

        private readonly System.EventHandler _childTransform_ChangedHandler;

        public Transforms()
        {
            _childTransform_ChangedHandler = (_, _) =>
                NotifyPropertyChangedEvent(IndexPropertyChanged);
        }

        public override void Insert(int index, Transform item)
        {
            base.Insert(index, item);
            item.Changed += _childTransform_ChangedHandler;
        }

        public override bool Remove(Transform item)
        {
            item.Changed -= _childTransform_ChangedHandler;
            return base.Remove(item);
        }

        public new Transform this[int index]
        {
            get => base[index];
            set
            {
                if (base[index] is Transform old)
                {
                    old.Changed -= _childTransform_ChangedHandler;
                }
                if (value is Transform transform)
                {
                    transform.Changed += _childTransform_ChangedHandler;
                }
                base[index] = value;
                NotifyPropertyChangedEvent(IndexPropertyChanged);
            }
        }
    }
}
