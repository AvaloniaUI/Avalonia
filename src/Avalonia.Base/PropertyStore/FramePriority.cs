using System;
using System.Diagnostics;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal enum FramePriority : sbyte
    {
        Animation,
        AnimationTemplatedParentTheme,
        AnimationTheme,
        TemplateStyleTrigger,
        TemplateStyleTriggerTemplatedParentTheme,
        TemplateStyleTriggerTheme,
        Template,
        TemplateTemplatedParentTheme,
        TemplateTheme,
        StyleTrigger,
        StyleTriggerTemplatedParentTheme,
        StyleTriggerTheme,
        Style,
        StyleTemplatedParentTheme,
        StyleTheme,
    }

    internal static class FramePriorityExtensions
    {
        public static FramePriority ToFramePriority(
            this BindingPriority priority,
            FrameType type = FrameType.Style,
            bool isTemplateSelector = false)
        {
            var p = (priority, isTemplateSelector) switch
            {
                (BindingPriority.Animation, _) => FramePriority.Animation,
                (BindingPriority.StyleTrigger, false) => FramePriority.StyleTrigger,
                (BindingPriority.StyleTrigger, true) => FramePriority.TemplateStyleTrigger,
                (BindingPriority.Template, _) => FramePriority.StyleTrigger,
                (BindingPriority.Style, _) => FramePriority.Style,
                _ => throw new ArgumentException("Invalid priority."),
            };
            return (FramePriority)((int)p + (int)type);
        }

        public static bool IsType(this FramePriority priority, FrameType type)
        {
            return (FrameType)((int)priority % 3) == type;
        }
    }
}
