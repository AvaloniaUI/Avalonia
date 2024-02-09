using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Avalonia.Media;

/// <summary>
/// Font feature
/// </summary>
public record FontFeature
{
    private const int DefaultValue = 1;
    private const int InfinityEnd = -1;
    
    private static readonly Regex s_featureRegex = new Regex(
        @"^\s*(?<Value>[+-])?\s*(?<Tag>\w{4})\s*(\[\s*(?<Start>\d+)?(\s*(?<Separator>:)\s*)?(?<End>\d+)?\s*\])?\s*(?(Value)()|(=\s*(?<Value>\d+|on|off)))?\s*$", 
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    /// <summary>Gets or sets the tag.</summary>
    public string Tag
    {
        get;
        init;
    }

    /// <summary>Gets or sets the value.</summary>
    public int Value
    {
        get;
        init;
    }

    /// <summary>Gets or sets the start.</summary>
    public int Start
    {
        get;
        init;
    }

    /// <summary>Gets or sets the end.</summary>
    public int End
    {
        get;
        init;
    }
    
    /// <summary>
    /// Creates an instance of FontFeature.
    /// </summary>
    public FontFeature()
    {
        Tag = string.Empty;
        Value = DefaultValue;
        Start = 0;
        End = InfinityEnd;
    }

    /// <summary>
    /// Parses a string to return a <see cref="FontFeature"/>.
    /// Syntax is the following:
    ///  
    ///     Syntax 	        Value 	Start 	End 	 
    ///     Setting value: 	  	  	  	 
    ///     kern 	        1 	    0 	    ∞ 	    Turn feature on
    ///     +kern 	        1 	    0 	    ∞ 	    Turn feature on
    ///     -kern 	        0 	    0 	    ∞ 	    Turn feature off
    ///     kern=0 	        0 	    0 	    ∞ 	    Turn feature off
    ///     kern=1 	        1 	    0 	    ∞ 	    Turn feature on
    ///     aalt=2 	        2 	    0 	    ∞ 	    Choose 2nd alternate
    ///     Setting index: 	  	  	  	 
    ///     kern[] 	        1 	    0 	    ∞ 	    Turn feature on
    ///     kern[:] 	    1 	    0 	    ∞ 	    Turn feature on
    ///     kern[5:] 	    1 	    5 	    ∞ 	    Turn feature on, partial
    ///     kern[:5] 	    1 	    0 	    5 	    Turn feature on, partial
    ///     kern[3:5] 	    1 	    3 	    5 	    Turn feature on, range
    ///     kern[3] 	    1 	    3 	    3+1 	Turn feature on, single char
    ///     Mixing it all: 	  	  	  	 
    ///     aalt[3:5]=2 	2 	    3 	    5 	    Turn 2nd alternate on for range
    /// 
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="FontFeature"/>.</returns>
    // ReSharper disable once UnusedMember.Global
    public static FontFeature Parse(string s)
    {
        var match = s_featureRegex.Match(s);
        
        if (!match.Success)
        {
            return new FontFeature();
        }
           
        var hasSeparator = match.Groups["Separator"].Value == ":";
        var hasStart = int.TryParse(match.Groups["Start"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var start);
        var hasEnd = int.TryParse(match.Groups["End"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var end);
        
        var stringValue = match.Groups["Value"].Value;
        if (stringValue == "-" || stringValue.ToUpperInvariant() == "OFF")
            stringValue = "0";
        if (stringValue == "+" || stringValue.ToUpperInvariant() == "ON")
            stringValue = "1";

        var result = new FontFeature
        {
            Tag = match.Groups["Tag"].Value,
            Start = hasStart ? start : 0,
            End = hasEnd ? end : hasStart && !hasSeparator ? (start + 1) : InfinityEnd,
            Value = int.TryParse(stringValue, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : DefaultValue,
        };

        return result;
    }
    
    /// <summary>
    /// Gets a string representation of the <see cref="FontFeature"/>.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        var result = new StringBuilder(128);
        
        if (Value == 0)
            result.Append('-');
        result.Append(Tag ?? string.Empty);

        if (Start != 0 || End != InfinityEnd)
        {
            result.Append('[');
            
            if (Start > 0)
                result.Append(Start.ToString(CultureInfo.InvariantCulture));
            
            if (End != Start + 1) 
            {
                result.Append(':');
                if (End != InfinityEnd)
                    result.Append(End.ToString(CultureInfo.InvariantCulture));
            }
            
            result.Append(']');
        }

        if (Value is DefaultValue or 0)
        {
            return result.ToString();
        }
        
        result.Append('=');
        result.Append(Value.ToString(CultureInfo.InvariantCulture));

        return result.ToString();
    }
}
