// -----------------------------------------------------------------------
// <copyright file="TextBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Interactivity;

    public class TextBox : TemplatedControl
    {
        public static readonly PerspexProperty<bool> AcceptsReturnProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsReturn");

        public static readonly PerspexProperty<bool> AcceptsTabProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsTab");

        public static readonly PerspexProperty<int> CaretIndexProperty =
            PerspexProperty.Register<TextBox, int>("CaretIndex", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<int> SelectionStartProperty =
            PerspexProperty.Register<TextBox, int>("SelectionStart", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<int> SelectionEndProperty =
            PerspexProperty.Register<TextBox, int>("SelectionEnd", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<string> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>();

        public static readonly PerspexProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        private TextPresenter presenter;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            var canScrollHorizontally = this.GetObservable(AcceptsReturnProperty)
                .Select(x => !x);

            this.Bind(
                ScrollViewer.CanScrollHorizontallyProperty,
                canScrollHorizontally,
                BindingPriority.Style);

            var horizontalScrollBarVisibility = this.GetObservable(AcceptsReturnProperty)
                .Select(x => x ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden);

            this.Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty, 
                horizontalScrollBarVisibility, 
                BindingPriority.Style);
        }

        public bool AcceptsReturn
        {
            get { return this.GetValue(AcceptsReturnProperty); }
            set { this.SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return this.GetValue(AcceptsTabProperty); }
            set { this.SetValue(AcceptsTabProperty, value); }
        }

        public int CaretIndex
        {
            get { return this.GetValue(CaretIndexProperty); }
            set { this.SetValue(CaretIndexProperty, value); }
        }

        public int SelectionStart
        {
            get { return this.GetValue(SelectionStartProperty); }
            set { this.SetValue(SelectionStartProperty, value); }
        }

        public int SelectionEnd
        {
            get { return this.GetValue(SelectionEndProperty); }
            set { this.SetValue(SelectionEndProperty, value); }
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return this.GetValue(TextWrappingProperty); }
            set { this.SetValue(TextWrappingProperty, value); }
        }

        private static int CoerceCaretIndex(PerspexObject o, int value)
        {
            var text = o.GetValue(TextProperty);
            var length = (text != null) ? text.Length : 0;
            return Math.Max(0, Math.Min(length, value));
        }

        protected override void OnTemplateApplied()
        {
            this.presenter = this.GetTemplateChild<TextPresenter>("textPresenter");
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            // TODO: There needs to be a better way of setting focus to a templated child.
            base.OnGotFocus(e);
            FocusManager.Instance.Focus(this.presenter);
        }
    }
}
