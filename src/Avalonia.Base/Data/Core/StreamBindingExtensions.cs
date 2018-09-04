using System;
using System.Threading.Tasks;

namespace Avalonia
{
    public static class StreamBindingExtensions
    {
        internal static string StreamBindingName = "StreamBinding";

        public static T StreamBinding<T>(this Task<T> @this)
        {
            throw new InvalidOperationException("This should be used only in a binding expression");
        }

        public static object StreamBinding(this Task @this)
        {
            throw new InvalidOperationException("This should be used only in a binding expression");
        }

        public static T StreamBinding<T>(this IObservable<T> @this)
        {
            throw new InvalidOperationException("This should be used only in a binding expression");
        }
    }
}
