// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Styling;
    using Perspex.Threading;
    using Splat;

    public class Application
    {
        private DataTemplates dataTemplates;

        private Styles styles;

        public Application()
        {
            Current = this;
            this.FocusManager = new FocusManager();
            this.InputManager = new InputManager();
        }

        public static Application Current
        {
            get;
            private set;
        }

        public DataTemplates DataTemplates
        {
            get
            {
                if (this.dataTemplates == null)
                {
                    this.dataTemplates = new DataTemplates();
                }

                return this.dataTemplates;
            }

            set
            {
                this.dataTemplates = value;
            }
        }

        public IFocusManager FocusManager
        {
            get;
            private set;
        }

        public InputManager InputManager
        {
            get;
            private set;
        }

        public Styles Styles
        {
            get
            {
                if (this.styles == null)
                {
                    this.styles = new Styles();
                }

                return this.styles;
            }

            set
            {
                this.styles = value;
            }
        }

        public void RegisterServices()
        {
            Styler styler = new Styler();
            Locator.CurrentMutable.Register(() => this.FocusManager, typeof(IFocusManager));
            Locator.CurrentMutable.Register(() => this.InputManager, typeof(IInputManager));
            Locator.CurrentMutable.Register(() => styler, typeof(IStyler));
        }

        public void Run(ICloseable closable)
        {
            DispatcherFrame frame = new DispatcherFrame();
            closable.Closed += (s, e) => frame.Continue = false;
            Dispatcher.PushFrame(frame);
        }
    }
}
