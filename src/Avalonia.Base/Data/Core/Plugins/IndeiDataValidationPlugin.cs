using System.ComponentModel;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties on objects that implement <see cref="INotifyDataErrorInfo"/>.
    /// </summary>
    public class IndeiDataValidationPlugin : DataValidationPlugin
    {
        public override string Identifier => nameof(INotifyDataErrorInfo);

        public override bool Match(object source, string memberName)
        {
            return source is INotifyDataErrorInfo;
        }

        /// <inheritdoc/>
        public override MemberDataValidator Start(object source, string memberName)
        {
            return new IndeiDataValidator((INotifyDataErrorInfo)source, memberName);
        }
    }
}
