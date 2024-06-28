using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;

namespace Avalonia.Browser
{
    public class AvaloniaView
    {
        private readonly EmbeddableControlRoot _topLevel;

        /// <param name="divId">ID of the html element where avalonia content should be rendered.</param>
        public AvaloniaView(string divId)
            : this(DomHelper.GetElementById(divId, BrowserWindowingPlatform.GlobalThis) ??
                   throw new Exception($"Element with id '{divId}' was not found in the html document."))
        {
        }

        /// <param name="host">JSObject holding a div element where avalonia content should be rendered.</param>
        public AvaloniaView(JSObject host)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var hostContent = DomHelper.CreateAvaloniaHost(host);
            if (hostContent == null)
            {
                throw new InvalidOperationException("Avalonia WASM host wasn't initialized.");
            }

            var nativeControlsContainer = hostContent.GetPropertyAsJSObject("nativeHost")
                                          ?? throw new InvalidOperationException("NativeHost cannot be null");
            var inputElement = hostContent.GetPropertyAsJSObject("inputElement")
                               ?? throw new InvalidOperationException("InputElement cannot be null");

            var topLevelImpl = new BrowserTopLevelImpl(host, nativeControlsContainer, inputElement);
            _topLevel = new EmbeddableControlRoot(topLevelImpl);

            _topLevel.Prepare();
            _topLevel.GotFocus += (_, _) => InputHelper.FocusElement(host);
            _topLevel.Renderer.Start(); // TODO: use Start+StopRenderer() instead.
            _topLevel.RequestAnimationFrame(_ =>
            {
                // Try to get local splash-screen of the specific host.
                // If couldn't find - get global one by ID for compatibility.
                var splash = DomHelper.GetElementsByClassName("avalonia-splash", host)
                             ?? DomHelper.GetElementById("avalonia-splash", BrowserWindowingPlatform.GlobalThis);
                if (splash is not null)
                {
                    DomHelper.AddCssClass(splash, "splash-close");
                    splash.Dispose();
                }
            });
        }

        public Control? Content
        {
            get => (Control)_topLevel.Content!;
            set => _topLevel.Content = value;
        }

        internal TopLevel TopLevel => _topLevel;
    }
}
