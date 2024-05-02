using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Platform.Storage;

namespace Avalonia.Dialogs.Internal
{
    public class ManagedFileChooserFilterViewModel : AvaloniaDialogsInternalViewModelBase
    {
        private readonly Regex[]? _patterns;
        public string Name { get; }

        public ManagedFileChooserFilterViewModel(FilePickerFileType filter)
        {
            Name = filter.Name;

            if (filter.Patterns?.Contains("*.*") == true)
            {
                return;
            }

             _patterns = filter.Patterns?
                           .Select(e => new Regex("^" + Regex.Escape(e).Replace("\\*", ".*") + "$", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                           .ToArray();
        }

        public bool Match(string filename)
        {
            return _patterns == null || _patterns.Any(ext => ext.IsMatch(filename));
        }

        public override string ToString() => Name;
    }
}
