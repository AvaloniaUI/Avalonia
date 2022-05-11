﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.WinRT
{
    internal static class NativeWinRTMethods
    {
        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall,
            PreserveSig = false)]
        internal static extern unsafe IntPtr WindowsCreateString(
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            int length);

        internal static IntPtr WindowsCreateString(string sourceString) 
            => WindowsCreateString(sourceString, sourceString.Length);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", 
            CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        internal static extern unsafe void WindowsDeleteString(IntPtr hString);
        
        [DllImport("Windows.UI.Composition", EntryPoint = "DllGetActivationFactory",
            CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private extern static IntPtr GetWindowsUICompositionActivationFactory(
            IntPtr activatableClassId);

        internal static IActivationFactory GetWindowsUICompositionActivationFactory(string className)
        {//"Windows.UI.Composition.Compositor"
            var s = WindowsCreateString(className);
            var factory = GetWindowsUICompositionActivationFactory(s);
            return MicroComRuntime.CreateProxyFor<IActivationFactory>(factory, true);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate int GetActivationFactoryDelegate(IntPtr classId, out IntPtr ppv);
        
        internal static T CreateInstance<T>(string fullName) where T : IUnknown
        {
            var s = WindowsCreateString(fullName);
            EnsureRoInitialized();
            var pUnk = RoActivateInstance(s);
            using var unk = MicroComRuntime.CreateProxyFor<IUnknown>(pUnk, true);
            WindowsDeleteString(s);
            return MicroComRuntime.QueryInterface<T>(unk);
        }
        
        internal static TFactory CreateActivationFactory<TFactory>(string fullName) where TFactory : IUnknown
        {
            var s = WindowsCreateString(fullName);
            EnsureRoInitialized();
            var guid = MicroComRuntime.GetGuidFor(typeof(TFactory));
            var pUnk = RoGetActivationFactory(s, ref guid);
            using var unk = MicroComRuntime.CreateProxyFor<IUnknown>(pUnk, true);
            WindowsDeleteString(s);
            return MicroComRuntime.QueryInterface<TFactory>(unk);
        }
        
        internal enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        internal enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        [DllImport("coremessaging.dll", PreserveSig = false)]
        internal static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options);

        internal enum RO_INIT_TYPE
        {
            RO_INIT_SINGLETHREADED = 0, // Single-threaded application
            RO_INIT_MULTITHREADED = 1, // COM calls objects on any thread.
        }

        [DllImport("combase.dll", PreserveSig = false)]
        private static extern void RoInitialize(RO_INIT_TYPE initType);

        [DllImport("combase.dll", PreserveSig = false)]
        private static extern IntPtr RoActivateInstance(IntPtr activatableClassId);

        [DllImport("combase.dll", PreserveSig = false)]
        private static extern IntPtr RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid);
        
        private static bool _initialized;
        private static void EnsureRoInitialized()
        {
            if (_initialized)
                return;
            RoInitialize(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA ?
                RO_INIT_TYPE.RO_INIT_SINGLETHREADED :
                RO_INIT_TYPE.RO_INIT_MULTITHREADED);
            _initialized = true;
        }
    }

    class HStringInterop : IDisposable
    {
        private IntPtr _s;

        public HStringInterop(string s)
        {
            _s = s == null ? IntPtr.Zero : NativeWinRTMethods.WindowsCreateString(s);
        }

        public IntPtr Handle => _s;

        public void Dispose()
        {
            if (_s != IntPtr.Zero)
            {
                NativeWinRTMethods.WindowsDeleteString(_s);
                _s = IntPtr.Zero;
            }
        }
    }
}
