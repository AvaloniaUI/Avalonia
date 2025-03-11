using Avalonia.Data;
using Avalonia.PropertyStore;
using Xunit;

namespace Avalonia.Base.UnitTests.PropertyStore;

#pragma warning disable format

public class FramePriorityExtensionsTests
{

    [Theory]
    [InlineData(BindingPriority.Animation,    FrameType.Style,                FramePriority.Animation)]
    [InlineData(BindingPriority.Animation,    FrameType.TemplatedParentTheme, FramePriority.AnimationTemplatedParentTheme)]
    [InlineData(BindingPriority.Animation,    FrameType.Theme,                FramePriority.AnimationTheme)]
    [InlineData(BindingPriority.StyleTrigger, FrameType.Style,                FramePriority.StyleTrigger)]
    [InlineData(BindingPriority.StyleTrigger, FrameType.TemplatedParentTheme, FramePriority.StyleTriggerTemplatedParentTheme)]
    [InlineData(BindingPriority.StyleTrigger, FrameType.Theme,                FramePriority.StyleTriggerTheme)]
    [InlineData(BindingPriority.Template,     FrameType.Style,                FramePriority.Template)]
    [InlineData(BindingPriority.Template,     FrameType.TemplatedParentTheme, FramePriority.TemplateTemplatedParentTheme)]
    [InlineData(BindingPriority.Template,     FrameType.Theme,                FramePriority.TemplateTheme)]
    [InlineData(BindingPriority.Style,        FrameType.Style,                FramePriority.Style)]
    [InlineData(BindingPriority.Style,        FrameType.TemplatedParentTheme, FramePriority.StyleTemplatedParentTheme)]
    [InlineData(BindingPriority.Style,        FrameType.Theme,                FramePriority.StyleTheme)]
    internal void BindingPriority_To_FramePriority(BindingPriority priority, FrameType type, FramePriority expected)
    {
        Assert.Equal(expected, priority.ToFramePriority(type));
    }

    [Theory]
    [InlineData(FramePriority.Animation,                        FrameType.Style,                true)]
    [InlineData(FramePriority.StyleTrigger,                     FrameType.Style,                true)]
    [InlineData(FramePriority.Template,                         FrameType.Style,                true)]
    [InlineData(FramePriority.Style,                            FrameType.Style,                true)]
    [InlineData(FramePriority.AnimationTemplatedParentTheme,    FrameType.TemplatedParentTheme, true)]
    [InlineData(FramePriority.StyleTriggerTemplatedParentTheme, FrameType.TemplatedParentTheme, true)]
    [InlineData(FramePriority.TemplateTemplatedParentTheme,     FrameType.TemplatedParentTheme, true)]
    [InlineData(FramePriority.StyleTemplatedParentTheme,        FrameType.TemplatedParentTheme, true)]
    [InlineData(FramePriority.AnimationTheme,                   FrameType.Theme,                true)]
    [InlineData(FramePriority.StyleTriggerTheme,                FrameType.Theme,                true)]
    [InlineData(FramePriority.TemplateTheme,                    FrameType.Theme,                true)]
    [InlineData(FramePriority.StyleTheme,                       FrameType.Theme,                true)]
    //
    [InlineData(FramePriority.Style,                            FrameType.TemplatedParentTheme, false)]
    [InlineData(FramePriority.Style,                            FrameType.Theme,                false)]
    [InlineData(FramePriority.StyleTheme,                       FrameType.TemplatedParentTheme, false)]
    [InlineData(FramePriority.StyleTheme,                       FrameType.Style,                false)]
    internal void FramePriority_Is_FrameType(FramePriority priority, FrameType type, bool expected)
    {
        Assert.Equal(expected, priority.IsType(type));
    }
}
