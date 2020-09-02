using System;
using System.Diagnostics;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class PerformanceViewModel : ViewModelBase
    {
        private long _totalMemory;
        private long _managedMemory;
        
        public string TotalMemory
        {
            get => PrettyPrint(_totalMemory, 1);
        }

        public string ManagedMemory
        {
            get => PrettyPrint(_managedMemory, 1);
        }

        public void Update()
        {
            using (var p = Process.GetCurrentProcess())
            {
                p.Refresh();
                _totalMemory = p.PrivateMemorySize64;
                _managedMemory = GC.GetTotalMemory(true);
            }

            RaisePropertyChanged(nameof(TotalMemory));
            RaisePropertyChanged(nameof(ManagedMemory));
        }

        public static string PrettyPrint(long value, int decimalPlaces = 0)
        {
            const long OneKb = 1024;
            const long OneMb = OneKb * 1024;
            const long OneGb = OneMb * 1024;
            const long OneTb = OneGb * 1024;
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}TB", asTb)
                : asGb > 1 ? string.Format("{0}GB", asGb)
                : asMb > 1 ? string.Format("{0}MB", asMb)
                : asKb > 1 ? string.Format("{0}KB", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return $"{chosenValue} ({value:N0} bytes)";
        }
    }
}
