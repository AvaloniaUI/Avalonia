// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avalonia.Platform.Gpu
{
    /// <summary>
    /// OpenGL wrapper class. Bare minimum is exposed currently.
    /// </summary>
    public static class GL
    {
        public const int FRAMEBUFFER_BINDING = 0x8CA6;

        private static bool s_isInitialized;

        private static class Native
        {
            internal delegate void glGetIntegerv(int pname, out int data);

            internal static glGetIntegerv pglGetIntegerv;

            internal delegate void glViewport(int x, int y, int width, int height);

            internal static glViewport pglViewport;

            internal delegate void glFlush();

            internal static glFlush pglFlush;
        }

        public static void GetIntegerv(int name, int[] data)
        {
            Native.pglGetIntegerv(name, out data[0]);
        }

        public static void GetIntegerv(int name, out int data)
        {
            Native.pglGetIntegerv(name, out data);
        }

        public static void Flush()
        {
            Native.pglFlush();
        }

        public static void Viewport(int x, int y, int width, int height)
        {
            Native.pglViewport(x, y, width, height);
        }

        /// <summary>
        /// Initialize OpenGL binding with given loader.
        /// </summary>
        /// <param name="loader">Function pointer loader.</param>
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

        /// <summary>
        /// Load and bind function to <see cref="Native"/> field.
        /// </summary>
        /// <param name="loader">Loader to use.</param>
        /// <param name="field">Field to bind.</param>
        private static void BindApiFunction(Func<string, IntPtr> loader, FieldInfo field)
        {
            var functionName = field.FieldType.Name;
            var functionPointer = loader(functionName);
            var functionDelegate = Marshal.GetDelegateForFunctionPointer(functionPointer, field.FieldType);

            field.SetValue(null, functionDelegate);
        }
    }
}