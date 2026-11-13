namespace Avalonia.Media
{
    /// <summary>
    /// A layout constant of the OpenType MATH table.
    /// </summary>
    /// <remarks>
    /// The members are in MathConstants table order. <see cref="ScriptPercentScaleDown"/>,
    /// <see cref="ScriptScriptPercentScaleDown"/> and <see cref="RadicalDegreeBottomRaisePercent"/>
    /// are percentages; all others are lengths in design units.
    /// </remarks>
    public enum MathConstant
    {
        /// <summary>
        /// The percentage by which the first level of subscripts and superscripts is scaled down.
        /// </summary>
        ScriptPercentScaleDown,

        /// <summary>
        /// The percentage by which the second level of subscripts and superscripts is scaled down.
        /// </summary>
        ScriptScriptPercentScaleDown,

        /// <summary>
        /// The minimum height of a sub-formula for which delimiters are sized to it rather than left at their default size.
        /// </summary>
        DelimitedSubFormulaMinHeight,

        /// <summary>
        /// The minimum height of an n-ary operator, such as integral or summation, in display style.
        /// </summary>
        DisplayOperatorMinHeight,

        /// <summary>
        /// The white space to leave between lines of math.
        /// </summary>
        MathLeading,

        /// <summary>
        /// The height of the math axis above the baseline, around which fractions and delimiters are centered.
        /// </summary>
        AxisHeight,

        /// <summary>
        /// The maximum height of a base on which accents are placed without being raised.
        /// </summary>
        AccentBaseHeight,

        /// <summary>
        /// The minimum height of a base above which flattened accents are used.
        /// </summary>
        FlattenedAccentBaseHeight,

        /// <summary>
        /// The standard shift down of a subscript.
        /// </summary>
        SubscriptShiftDown,

        /// <summary>
        /// The maximum height of the top of a subscript above the baseline.
        /// </summary>
        SubscriptTopMax,

        /// <summary>
        /// The minimum distance of a subscript's baseline below the bottom of its base.
        /// </summary>
        SubscriptBaselineDropMin,

        /// <summary>
        /// The standard shift up of a superscript.
        /// </summary>
        SuperscriptShiftUp,

        /// <summary>
        /// The standard shift up of a superscript in cramped style.
        /// </summary>
        SuperscriptShiftUpCramped,

        /// <summary>
        /// The minimum height of the bottom of a superscript above the baseline.
        /// </summary>
        SuperscriptBottomMin,

        /// <summary>
        /// The maximum distance of a superscript's baseline below the top of its base.
        /// </summary>
        SuperscriptBaselineDropMax,

        /// <summary>
        /// The minimum gap between a superscript's bottom and a subscript's top.
        /// </summary>
        SubSuperscriptGapMin,

        /// <summary>
        /// The maximum height of a superscript's bottom above the baseline when it is raised to make room for a subscript.
        /// </summary>
        SuperscriptBottomMaxWithSubscript,

        /// <summary>
        /// The extra white space added after a subscript or superscript.
        /// </summary>
        SpaceAfterScript,

        /// <summary>
        /// The minimum gap between the bottom of an upper limit and the top of its base operator.
        /// </summary>
        UpperLimitGapMin,

        /// <summary>
        /// The minimum distance between the baseline of an upper limit and the top of its base operator.
        /// </summary>
        UpperLimitBaselineRiseMin,

        /// <summary>
        /// The minimum gap between the top of a lower limit and the bottom of its base operator.
        /// </summary>
        LowerLimitGapMin,

        /// <summary>
        /// The minimum distance between the baseline of a lower limit and the bottom of its base operator.
        /// </summary>
        LowerLimitBaselineDropMin,

        /// <summary>
        /// The standard shift up of the top element of a stack.
        /// </summary>
        StackTopShiftUp,

        /// <summary>
        /// The standard shift up of the top element of a stack in display style.
        /// </summary>
        StackTopDisplayStyleShiftUp,

        /// <summary>
        /// The standard shift down of the bottom element of a stack.
        /// </summary>
        StackBottomShiftDown,

        /// <summary>
        /// The standard shift down of the bottom element of a stack in display style.
        /// </summary>
        StackBottomDisplayStyleShiftDown,

        /// <summary>
        /// The minimum gap between the bottom of a stack's top element and the top of its bottom element.
        /// </summary>
        StackGapMin,

        /// <summary>
        /// The minimum gap between the elements of a stack in display style.
        /// </summary>
        StackDisplayStyleGapMin,

        /// <summary>
        /// The standard shift up of the top element of a stretch stack.
        /// </summary>
        StretchStackTopShiftUp,

        /// <summary>
        /// The standard shift down of the bottom element of a stretch stack.
        /// </summary>
        StretchStackBottomShiftDown,

        /// <summary>
        /// The minimum gap between the stretching element of a stretch stack and the element above it.
        /// </summary>
        StretchStackGapAboveMin,

        /// <summary>
        /// The minimum gap between the stretching element of a stretch stack and the element below it.
        /// </summary>
        StretchStackGapBelowMin,

        /// <summary>
        /// The standard shift up of a fraction's numerator.
        /// </summary>
        FractionNumeratorShiftUp,

        /// <summary>
        /// The standard shift up of a fraction's numerator in display style.
        /// </summary>
        FractionNumeratorDisplayStyleShiftUp,

        /// <summary>
        /// The standard shift down of a fraction's denominator.
        /// </summary>
        FractionDenominatorShiftDown,

        /// <summary>
        /// The standard shift down of a fraction's denominator in display style.
        /// </summary>
        FractionDenominatorDisplayStyleShiftDown,

        /// <summary>
        /// The minimum gap between the bottom of a fraction's numerator and the top of its rule.
        /// </summary>
        FractionNumeratorGapMin,

        /// <summary>
        /// The minimum gap between a fraction's numerator and its rule in display style.
        /// </summary>
        FractionNumDisplayStyleGapMin,

        /// <summary>
        /// The thickness of a fraction rule.
        /// </summary>
        FractionRuleThickness,

        /// <summary>
        /// The minimum gap between the bottom of a fraction's rule and the top of its denominator.
        /// </summary>
        FractionDenominatorGapMin,

        /// <summary>
        /// The minimum gap between a fraction's rule and its denominator in display style.
        /// </summary>
        FractionDenomDisplayStyleGapMin,

        /// <summary>
        /// The horizontal gap between the numerator and denominator of a skewed fraction.
        /// </summary>
        SkewedFractionHorizontalGap,

        /// <summary>
        /// The vertical gap between the numerator and denominator of a skewed fraction.
        /// </summary>
        SkewedFractionVerticalGap,

        /// <summary>
        /// The gap between an overbar and the top of its base.
        /// </summary>
        OverbarVerticalGap,

        /// <summary>
        /// The thickness of an overbar.
        /// </summary>
        OverbarRuleThickness,

        /// <summary>
        /// The extra white space above an overbar.
        /// </summary>
        OverbarExtraAscender,

        /// <summary>
        /// The gap between an underbar and the bottom of its base.
        /// </summary>
        UnderbarVerticalGap,

        /// <summary>
        /// The thickness of an underbar.
        /// </summary>
        UnderbarRuleThickness,

        /// <summary>
        /// The extra white space below an underbar.
        /// </summary>
        UnderbarExtraDescender,

        /// <summary>
        /// The gap between a radical's rule and the top of its radicand.
        /// </summary>
        RadicalVerticalGap,

        /// <summary>
        /// The gap between a radical's rule and its radicand in display style.
        /// </summary>
        RadicalDisplayStyleVerticalGap,

        /// <summary>
        /// The thickness of a radical's rule.
        /// </summary>
        RadicalRuleThickness,

        /// <summary>
        /// The extra white space above a radical's rule.
        /// </summary>
        RadicalExtraAscender,

        /// <summary>
        /// The horizontal space before a radical's degree.
        /// </summary>
        RadicalKernBeforeDegree,

        /// <summary>
        /// The horizontal space after a radical's degree, usually negative.
        /// </summary>
        RadicalKernAfterDegree,

        /// <summary>
        /// The height of the bottom of a radical's degree as a percentage of the radical's height.
        /// </summary>
        RadicalDegreeBottomRaisePercent,
    }
}
