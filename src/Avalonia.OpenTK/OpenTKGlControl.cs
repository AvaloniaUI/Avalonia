using System;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;

namespace Avalonia.OpenTK
{
    public abstract class OpenTKGlControl : OpenGlControlBase
    {
        public OpenTKGlControl() : this(new OpenGlControlSettings())
        {
        }
        
        public OpenTKGlControl(OpenGlControlSettings settings) : base(UpdateSettings(settings))
        {
        }

        private static OpenGlControlSettings UpdateSettings(OpenGlControlSettings settings)
        {
            settings = settings.Clone();
            if (settings.Context == null && settings.ContextFactory == null)
            {
                settings.ContextFactory = () => AvaloniaOpenTKIntegration.CreateCompatibleContext(null);
            }

            return settings;
        }
    }
}
