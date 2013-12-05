using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, Func<object>> services =
            new Dictionary<Type, Func<object>>();

        public static T Get<T>()
        {
            return (T)services[typeof(T)]();
        }

        public static void Register<T>(Func<T> func)
        {
            services.Add(typeof(T), () => (object)func());
        }
    }
}
