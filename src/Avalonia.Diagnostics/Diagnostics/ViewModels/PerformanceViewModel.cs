using System;
using System.Diagnostics;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Diagnostics.ViewModels
{
    internal class PerformanceViewModel : ViewModelBase
    {
        private readonly IGraphicsMemoryDiagnostics? _gpu;
        private long _totalMemory;
        private long _managedMemory;
        private long? _gpuMemory;

        public PerformanceViewModel(IGraphicsMemoryDiagnostics? gpu)
        {
            _gpu = gpu;
        }

        public string TotalMemory
        {
            get => PrettyPrint(_totalMemory, 1);
        }

        public string ManagedMemory
        {
            get => PrettyPrint(_managedMemory, 1);
        }

        public string GraphicsMemory
        {
            get => _gpuMemory is object ? PrettyPrint(_gpuMemory.Value, 1) : "N/A";
        }

        public string DarkMatter
        {
            get => PrettyPrint(_totalMemory - _managedMemory - (_gpuMemory ?? 0));
        }

        public void Update()
        {
            using (var p = Process.GetCurrentProcess())
            {
                p.Refresh();
                _totalMemory = p.PrivateMemorySize64;
                _managedMemory = GC.GetTotalMemory(true);
                _gpuMemory = (long?)_gpu?.GetResourceUsage();
            }

            RaisePropertyChanged(nameof(TotalMemory));
            RaisePropertyChanged(nameof(ManagedMemory));
            RaisePropertyChanged(nameof(GraphicsMemory));
            RaisePropertyChanged(nameof(DarkMatter));
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
