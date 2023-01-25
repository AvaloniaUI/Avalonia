using System;

namespace Avalonia.Utilities
{
    public class NonPumpingLockHelper
    {
        public interface IHelperImpl
        {
            IDisposable? Use();
        }

        public static IDisposable? Use() => AvaloniaLocator.Current.GetService<IHelperImpl>()?.Use();
    }
}
