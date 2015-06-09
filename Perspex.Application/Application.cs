// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Threading;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Perspex.Threading;
    using Splat;

    /// <summary>
    /// Encapsulates a Perspex application.
    /// </summary>
    /// <remarks>
    /// The <see cref="Application"/> class encapsulates Perspex application-specific
    /// functionality, including:
    /// - A global set of <see cref="DataTemplates"/>.
    /// - A global set of <see cref="Styles"/>.
    /// - A <see cref="FocusManager"/>.
    /// - An <see cref="InputManager"/>.
    /// - Registers services needed by the rest of Perspex in the <see cref="RegisterServices"/>
    /// method.
    /// - Tracks the lifetime of the application.
    /// </remarks>
    public class Application : IGlobalDataTemplates, IGlobalStyles
    {
        /// <summary>
        /// The application-global data templates.
        /// </summary>
        private DataTemplates dataTemplates;

        /// <summary>
        /// The styler that will be used to apply styles to controls.
        /// </summary>
        private Styler styler = new Styler();

        private ICloseable mainWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="theme">The theme to use.</param>
        public Application(Styles theme)
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Cannot create more than one Application instance.");
            }

            Current = this;
            this.Styles = theme;
            this.RegisterServices();
        }

        /// <summary>
        /// Gets the current instance of the <see cref="Application"/> class.
        /// </summary>
        /// <value>
        /// The current instance of the <see cref="Application"/> class.
        /// </value>
        public static Application Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
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

        /// <summary>
        /// Gets the application's focus manager.
        /// </summary>
        /// <value>
        /// The application's focus manager.
        /// </value>
        public IFocusManager FocusManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public InputManager InputManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styles Styles
        {
            get;
            private set;
        }

        /// <summary>
        /// Runs the application's main loop until the <see cref="ICloseable"/> is closed.
        /// </summary>
        /// <param name="closable">The closable to track</param>
        public void Run(ICloseable closable)
        {
            var source = new CancellationTokenSource();
            closable.Closed += (s, e) => source.Cancel();
            Dispatcher.UIThread.MainLoop(source.Token);
        }

        /// <summary>
        /// Register's the services needed by Perspex.
        /// </summary>
        protected virtual void RegisterServices()
        {
            this.FocusManager = new FocusManager();
            this.InputManager = new InputManager();

            Locator.CurrentMutable.Register(() => new AccessKeyHandler(), typeof(IAccessKeyHandler));
            Locator.CurrentMutable.Register(() => this, typeof(IGlobalDataTemplates));
            Locator.CurrentMutable.Register(() => this, typeof(IGlobalStyles));
            Locator.CurrentMutable.Register(() => this.FocusManager, typeof(IFocusManager));
            Locator.CurrentMutable.Register(() => this.InputManager, typeof(IInputManager));
            Locator.CurrentMutable.Register(() => new KeyboardNavigationHandler(), typeof(IKeyboardNavigationHandler));
            Locator.CurrentMutable.Register(() => this.styler, typeof(IStyler));
            Locator.CurrentMutable.Register(() => new LayoutManager(), typeof(ILayoutManager));
            Locator.CurrentMutable.Register(() => new RenderManager(), typeof(IRenderManager));
        }
    }
}
