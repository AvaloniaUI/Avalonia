using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Documents;
using Avalonia.Threading;

namespace Avalonia.Controls;

/// <summary>
/// Configures and computes text scaling. This is an accessibility feature which allows the user to request that text be 
/// drawn at a different size than normal, without altering other UI elements. The default scaling algorithm is determined 
/// by the platform and may make text smaller as well as larger.
/// </summary>
/// <remarks>
/// Text scaling is applied when text is measured. It does not modify the value of <see cref="TextElement.FontSize"/>.
/// </remarks>
/// <seealso cref="ITextScaleable"/>
/// <seealso cref="ITextScaler"/>
public static class TextScaling
{
    private static readonly ConditionalWeakTable<ITextScaler, HashSet<Visual>> s_customTextScalerSubscribers = [];

    /// <summary>
    /// Determines whether <see cref="TextElement.FontSize"/> (along with <see cref="TextElement.LetterSpacing"/>, <see cref="TextBlock.LineHeight"/>, and 
    /// <see cref="TextBlock.LineSpacing"/>) should be scalled by calling <see cref="GetScaledFontSize"/> when measuring text content. The value is inherited.
    /// </summary>
    /// <remarks>Text scaling is typically not uniform. Smaller text scales up faster than larger text.</remarks>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Visual, bool>("IsEnabled", typeof(TextScaling), inherits: true);

    /// <summary>
    /// Determines the minimum size (in em units) to which text will be scaled by <see cref="GetScaledFontSize"/>. The value is inherited.
    /// </summary>
    /// <remarks>This value is used only when <see cref="IsEnabledProperty"/> is true.</remarks>
    public static readonly AttachedProperty<double> MinFontSizeProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("MinFontSize", typeof(TextScaling), inherits: true, validate: size => size >= 0);

    /// <summary>
    /// Determines the maximum size (in em units) to which text will be scaled by <see cref="GetScaledFontSize"/>. The value is inherited.
    /// </summary>
    /// <remarks>This value is used only when <see cref="IsEnabledProperty"/> is true.</remarks>
    public static readonly AttachedProperty<double> MaxFontSizeProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("MaxFontSize", typeof(TextScaling), defaultValue: double.PositiveInfinity, inherits: true,
            validate: size => size > 0);

    /// <summary>
    /// Determines a user-defined text scaling algorithm, which overrides platform text scaling in <see cref="GetScaledFontSize"/>. The value is inherited.
    /// </summary>
    /// <remarks>This value is used only when <see cref="IsEnabledProperty"/> is true.</remarks>
    public static readonly AttachedProperty<ITextScaler?> CustomTextScalerProperty =
        AvaloniaProperty.RegisterAttached<Visual, ITextScaler?>("CustomTextScaler", typeof(TextScaling), inherits: true);
  
    /// <inheritdoc cref="IsEnabledProperty"/> <see cref="IsEnabledProperty"/>
    public static bool GetIsEnabled(Visual visual) => visual.GetValue(IsEnabledProperty);
    /// <inheritdoc cref="IsEnabledProperty"/> <see cref="IsEnabledProperty"/>
    public static void SetIsEnabled(Visual visual, bool value) => visual.SetValue(IsEnabledProperty, value);
    /// <inheritdoc cref="MinFontSizeProperty"/> <see cref="MinFontSizeProperty"/>
    public static double GetMinFontSize(Visual visual) => visual.GetValue(MinFontSizeProperty);
    /// <inheritdoc cref="MinFontSizeProperty"/> <see cref="MinFontSizeProperty"/>
    public static void SetMinFontSize(Visual visual, double value) => visual.SetValue(MinFontSizeProperty, value);
    /// <inheritdoc cref="MaxFontSizeProperty"/> <see cref="MaxFontSizeProperty"/>
    public static double GetMaxFontSize(Visual visual) => visual.GetValue(MaxFontSizeProperty);
    /// <inheritdoc cref="MaxFontSizeProperty"/> <see cref="MaxFontSizeProperty"/>
    public static void SetMaxFontSize(Visual visual, double value) => visual.SetValue(MaxFontSizeProperty, value);
    /// <inheritdoc cref="CustomTextScalerProperty"/> <see cref="CustomTextScalerProperty"/>
    public static ITextScaler? GetCustomTextScaler(Visual visual) => visual.GetValue(CustomTextScalerProperty);
    /// <inheritdoc cref="CustomTextScalerProperty"/> <see cref="CustomTextScalerProperty"/>
    public static void SetCustomTextScaler(Visual visual, ITextScaler? value) => visual.SetValue(CustomTextScalerProperty, value);

    static TextScaling()
    {
        CustomTextScalerProperty.Changed.AddClassHandler<Visual>(OnCustomTextScalerChanged);
    }

    private static void OnCustomTextScalerChanged(Visual visual, AvaloniaPropertyChangedEventArgs args)
    {
        if (visual is not ITextScaleable)
        {
            return;
        }

        var (oldScaler, newScaler) = args.GetOldAndNewValue<ITextScaler?>();

        if (oldScaler != null && s_customTextScalerSubscribers.TryGetValue(oldScaler, out var oldSubscribers))
        {
            oldSubscribers.Remove(visual);
        }

        if (newScaler != null)
        {
            if (s_customTextScalerSubscribers.TryGetValue(newScaler, out var newSubscribers))
            {
                newSubscribers.Add(visual);
            }
            else
            {
                s_customTextScalerSubscribers.Add(newScaler, [visual]);
                newScaler.TextScalingChanged += OnCustomTextScalingChanged;
            }
        }
    }

    private static void OnCustomTextScalingChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (sender is ITextScaler scaler && s_customTextScalerSubscribers.TryGetValue(scaler, out var subscribers))
        {
            foreach (var visual in subscribers)
            {
                if (GetIsEnabled(visual))
                {
                    ((ITextScaleable)visual).OnTextScalingChanged();
                }
            }
        }
    }

    /// <summary>
    /// Scales <paramref name="baseFontSize"/> according to either the current platform text scaling rules, or the rules
    /// defined by the object assigned to <see cref="CustomTextScalerProperty"/> for <paramref name="visual"/>.
    /// </summary>
    /// <remarks>The values of <see cref="IsEnabledProperty"/>, <see cref="MinFontSizeProperty"/>, and <see cref="MaxFontSizeProperty"/>
    /// are enforced, even when <see cref="CustomTextScalerProperty"/> is in use.</remarks>
    public static double GetScaledFontSize(Visual visual, double baseFontSize)
    {
        if (double.IsNaN(baseFontSize) || baseFontSize <= 0 || !GetIsEnabled(visual) || 
            (GetCustomTextScaler(visual) ?? TopLevel.GetTopLevel(visual)?.PlatformSettings) is not { } scaler)
        {
            return baseFontSize;
        }

        // Extend min/max to encompass the base size. Conveniently, this means that min > max is not possible.
        var min = Math.Min(GetMinFontSize(visual), baseFontSize);
        var max = Math.Max(GetMaxFontSize(visual), baseFontSize);

        return Math.Clamp(scaler.GetScaledFontSize(visual, baseFontSize), min, max);
    }
}
