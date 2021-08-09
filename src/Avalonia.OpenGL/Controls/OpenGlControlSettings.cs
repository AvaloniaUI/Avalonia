using System;

namespace Avalonia.OpenGL.Controls
{
    public class OpenGlControlSettings
    {
        /// <summary>
        /// An external context that's managed outside of OpenGlControlBase
        /// </summary>
        public IGlContext Context { get; set; }
        
        /// <summary>
        /// A factory that creates a context to render with
        /// </summary>
        public Func<IGlContext> ContextFactory { get; set; }

        /// <summary>
        /// Automatically attach a compatible renderbuffer to GL_DEPTH_ATTACHMENT
        /// </summary>
        public bool DepthBufferAutomaticManagement { get; set; } = true;

        /// <summary>
        /// Automatically cleanup resources when the control is no longer attached to a window 
        /// </summary>
        public bool DeInitializeOnVisualTreeDetachment { get; set; } = true;

        /// <summary>
        /// Continuously render frames when control is on screen. If this setting is false (default),
        /// you need to call InvalidateVisual manually 
        /// </summary>
        public bool ContinuouslyRender { get; set; }

        /// <summary>
        /// Creates a copy of OpenGLControlSettings
        /// </summary>
        public OpenGlControlSettings Clone()
        {
            return new OpenGlControlSettings
            {
                Context = Context,
                ContextFactory = ContextFactory,
                DepthBufferAutomaticManagement = DepthBufferAutomaticManagement,
                DeInitializeOnVisualTreeDetachment = DepthBufferAutomaticManagement,
                ContinuouslyRender = ContinuouslyRender
            };
        }
    }
}
