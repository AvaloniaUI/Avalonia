// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Perspex.Input;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Styling;
    using Splat;

    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight,
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : TopLevel, IStyleable, IFocusScope
    {
        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly PerspexProperty<SizeToContent> SizeToContentProperty =
            PerspexProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Defines the <see cref="Title"/> property.
        /// </summary>
        public static readonly PerspexProperty<string> TitleProperty =
            PerspexProperty.Register<Window, string>(nameof(Title), "Window");

        private object dialogResult;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>(x => x.TitleChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
            : base(Locator.Current.GetService<IWindowImpl>())
        {
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IWindowImpl PlatformImpl
        {
            get { return (IWindowImpl)base.PlatformImpl; }
        }

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        public SizeToContent SizeToContent
        {
            get { return this.GetValue(SizeToContentProperty); }
            set { this.SetValue(SizeToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get { return this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        /// <inheritdoc/>
        Type IStyleable.StyleKey
        {
            get { return typeof(Window); }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            this.PlatformImpl.Dispose();
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}"/> method, the
        /// resulting task will produce the <see cref="dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object dialogResult)
        {
            this.dialogResult = dialogResult;
            this.Close();
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public void Hide()
        {
            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Hide();
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public void Show()
        {
            this.LayoutManager.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Show();
            }
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog()
        {
            return this.ShowDialog<object>();
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <returns>.
        /// A task that can be used to retrive the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>()
        {
            this.LayoutManager.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                var modal = this.PlatformImpl.ShowDialog();
                var result = new TaskCompletionSource<TResult>();

                Observable.FromEventPattern(this, nameof(this.Closed))
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        modal.Dispose();
                        result.SetResult((TResult)this.dialogResult);
                    });

                return result.Task;
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = this.SizeToContent;
            var size = this.ClientSize;
            var desired = base.MeasureOverride(availableSize);

            switch (sizeToContent)
            {
                case SizeToContent.Width:
                    size = new Size(desired.Width, this.ClientSize.Height);
                    break;
                case SizeToContent.Height:
                    size = new Size(this.ClientSize.Width, desired.Height);
                    break;
                case SizeToContent.WidthAndHeight:
                    size = new Size(desired.Width, desired.Height);
                    break;
                case SizeToContent.Manual:
                    size = this.ClientSize;
                    break;
                default:
                    throw new InvalidOperationException("Invalid value for SizeToContent.");
            }

            return size;
        }

        /// <inheritdoc/>
        protected override void HandleResized(Size clientSize)
        {
            if (!this.AutoSizing)
            {
                this.SizeToContent = SizeToContent.Manual;
            }

            base.HandleResized(clientSize);
        }

        private void TitleChanged(PerspexPropertyChangedEventArgs e)
        {
            this.PlatformImpl.SetTitle((string)e.NewValue);
        }
    }
}
