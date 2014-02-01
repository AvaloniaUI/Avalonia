// -----------------------------------------------------------------------
// <copyright file="ServiceLocator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;

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
