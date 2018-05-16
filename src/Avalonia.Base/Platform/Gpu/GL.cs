// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avalonia.Platform.Gpu
{
    public static class GL
    {
        public const int FRAMEBUFFER_BINDING = 0x8CA6;

        private static bool s_isInitialized;

        private static class Native
        {
            internal delegate void glGetIntegerv(int pname, int[] data);

            internal static glGetIntegerv pglGetIntegerv;
        }

        public static void GetIntegerv(int name, int[] data)
        {
            Native.pglGetIntegerv(name, data);
        }

        public static void Initialize(Func<string, IntPtr> loader)
        {
            if (s_isInitialized)
            {
                return;
            }

            var delegatesType = typeof(Native);

            var fields = delegatesType.GetTypeInfo().GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                BindApiFunction(loader, field);
            }

            s_isInitialized = true;
        }

        private static void BindApiFunction(Func<string, IntPtr> loader, FieldInfo field)
        {
            var functionName = field.FieldType.Name;
            var functionPointer = loader(functionName);
            var functionDelegate = Marshal.GetDelegateForFunctionPointer(functionPointer, field.FieldType);

            field.SetValue(null, functionDelegate);
        }
    }
}