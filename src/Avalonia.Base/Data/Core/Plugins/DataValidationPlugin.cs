using System.Collections.Generic;

namespace Avalonia.Data.Core.Plugins;

public abstract class DataValidationPlugin
{
    private static readonly List<DataValidationPlugin> s_registered =
    [
        new IndeiDataValidationPlugin()
    ];

    /// <summary>
    /// Gets the unique identifier associated with the plugin.
    /// </summary>
    /// <remarks>
    /// This will typically be the fully qualified name of the data validation feature
    /// implemented by the plugin, e.g. "INotifyDataErrorInfo", "DataAnnotations".
    /// </remarks>
    public abstract string Identifier { get; }

    /// <summary>
    /// Checks whether this plugin can handle data validation of the specified member.
    /// </summary>
    /// <param name="o">The object to validate.</param>
    /// <param name="memberName">The name of the member to validate.</param>
    /// <returns>True if the plugin can handle the object; otherwise false.</returns>
    public abstract bool Match(object o, string memberName);

    /// <summary>
    /// Starts monitoring the data validation state of a member.
    /// </summary>
    /// <param name="o">The object to validate.</param>
    /// <param name="memberName">The name of the member to validate.</param>
    /// <returns>
    /// A <see cref="MemberDataValidator"/> which can be used to monitor the data validation
    /// state.
    /// </returns>
    public abstract MemberDataValidator Start(object o, string memberName);

    public static MemberDataValidator? GetDataValidator(object source, string memberName)
    {
        foreach (var plugin in s_registered)
        {
            if (plugin.Match(source, memberName))
                return plugin.Start(source, memberName);
        }

        return null;
    }
}
