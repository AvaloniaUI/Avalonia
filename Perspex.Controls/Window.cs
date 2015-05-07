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
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Styling;
    using Splat;

    public class Window : TopLevel, IStyleable
    {
        public static readonly PerspexProperty<string> TitleProperty =
            PerspexProperty.Register<Window, string>("Title", "Window");

        private object dialogResult;

        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
        }

        public Window()
            : base(Locator.Current.GetService<IWindowImpl>())
        {
        }

        public new IWindowImpl PlatformImpl
        {
            get { return (IWindowImpl)base.PlatformImpl; }
        }

        public string Title
        {
            get { return this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        Type IStyleable.StyleKey
        {
            get { return typeof(Window); }
        }

        public void Close()
        {
            this.PlatformImpl.Dispose();
        }

        public void Close(object dialogResult)
        {
            this.dialogResult = dialogResult;
            this.Close();
        }

        public void Hide()
        {
            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Hide();
            }
        }

        public void Show()
        {
            this.LayoutManager.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Show();
            }
        }

        public Task ShowDialog()
        {
            return this.ShowDialog<object>();
        }

        public Task<TResult> ShowDialog<TResult>()
        {
            this.LayoutManager.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                var modal = this.PlatformImpl.ShowDialog();
                var result = new TaskCompletionSource<TResult>();

                Observable.FromEventPattern(this, nameof(Closed))
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        modal.Dispose();
                        result.SetResult((TResult)this.dialogResult);
                    });

                return result.Task;
            }
        }
    }
}
