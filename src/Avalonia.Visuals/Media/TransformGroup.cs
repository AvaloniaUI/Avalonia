// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Collections;
using Avalonia.Metadata;
using System.Collections.Specialized;

namespace Avalonia.Media
{
    public class TransformGroup : Transform
    {
        /// <summary>
        /// Defines the <see cref="Children"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<Transforms> ChildrenProperty =
            AvaloniaProperty.Register<TransformGroup, Transforms>(nameof(Children));

        public TransformGroup()
        {
            Children = new Transforms();
            Children.CollectionChanged += (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (Transform tr in e.NewItems)
                        {
                            tr.Changed += ChildTransform_Changed;
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (Transform tr in e.OldItems)
                        {
                            tr.Changed -= ChildTransform_Changed;
                        }
                        break;
                }
            };
        }

        private void ChildTransform_Changed(object sender, System.EventArgs e)
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
        /// Gets the tranform's <see cref="Matrix" />.
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
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
    }
}