// -----------------------------------------------------------------------
// <copyright file="ContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Controls.Primitives;
    using Perspex.Media;

    public class ContentPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private bool createdChild;

        private Control child;

        private BehaviorSubject<Control> childObservable = new BehaviorSubject<Control>(null);

        public ContentPresenter()
        {
            this.GetObservable(ContentProperty).Skip(1).Subscribe(this.ContentChanged);
        }

        public Control Child
        {
            get
            {
                return this.child;
            }

            private set
            {
                if (this.child != value)
                {
                    this.ClearVisualChildren();
                    this.child = value;

                    if (value != null)
                    {
                        this.AddVisualChild(value);
                    }

                    this.childObservable.OnNext(value);
                }
            }
        }

        public IObservable<Control> ChildObservable
        {
            get { return this.childObservable; }
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        public override sealed void ApplyTemplate()
        {
            if (!this.createdChild)
            {
                this.CreateChild();
            }
        }

        protected override Size MeasureCore(Size availableSize)
        {
            return base.MeasureCore(availableSize);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.child != null)
            {
                this.child.Measure(availableSize);
                return this.child.DesiredSize.Value;
            }

            return new Size();
        }

        private void ContentChanged(object content)
        {
            this.createdChild = false;
            this.InvalidateMeasure();
        }

        private void CreateChild()
        {
            Control result = null;
            object content = this.Content;

            this.ClearVisualChildren();

            if (content != null)
            {

                if (content is Control)
                {
                    result = (Control)content;
                }
                else
                {
                    DataTemplate dataTemplate = this.FindDataTemplate(content);

                    if (dataTemplate != null)
                    {
                        result = dataTemplate.Build(content);
                    }
                    else
                    {
                        result = new TextBlock
                        {
                            Text = content.ToString(),
                        };
                    }
                }

                var foo = this.TemplatedParent as TemplatedControl;

                if (foo != null)
                {
                    foo = foo.TemplatedParent as TemplatedControl;
                }

                result.TemplatedParent = foo;
            }

            this.Child = result;
            this.createdChild = true;
        }
    }
}
