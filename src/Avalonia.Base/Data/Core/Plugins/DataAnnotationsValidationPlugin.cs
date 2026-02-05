using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties on that have <see cref="ValidationAttribute"/>s.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.DataValidationPluginRequiresUnreferencedCodeMessage)]
    public class DataAnnotationsValidationPlugin : DataValidationPlugin
    {
        override public string Identifier => "DataAnnotations";

        public override bool Match(object source, string memberName)
        {
            return source
                .GetType()
                .GetRuntimeProperty(memberName)?
                .GetCustomAttributes<ValidationAttribute>()
                .Any() ?? false;
        }

        public override MemberDataValidator Start(object source, string memberName)
        {
            return new DataAnnotationsValidator(source, memberName);
        }
    }
}
