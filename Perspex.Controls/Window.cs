// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public class Window : TopLevel
    {
        public static readonly PerspexProperty<string> TitleProperty =
            PerspexProperty.Register<Window, string>("Title", "Window");

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

        public void Hide()
        {
            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Hide();
            }
        }

        public void Show()
        {
            this.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                this.PlatformImpl.Show();
            }
        }
    }
}
