using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace Avalonia.Dialogs
{
    internal class ManagedFileChooserFilterViewModel : InternalViewModelBase
    {
        private readonly string[] _extensions;
        public string Name { get; }

        public ManagedFileChooserFilterViewModel(FileDialogFilter filter)
        {
            Name = filter.Name;

            if (filter.Extensions.Contains("*"))
            {
                return;
            }

            _extensions = filter.Extensions?.Select(e => "." + e.ToLowerInvariant()).ToArray();
        }

        public ManagedFileChooserFilterViewModel()
        {
            Name = "All files";
        }

        public bool Match(string filename)
        {
            if (_extensions == null)
            {
                return true;
            }

            foreach (var ext in _extensions)
            {
                if (filename.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() => Name;
    }
}
