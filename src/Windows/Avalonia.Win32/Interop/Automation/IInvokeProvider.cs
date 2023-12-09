// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Invoke pattern provider interface

using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInvokeProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("54fcb24b-e18e-47a2-b4d3-eccbe77599a2");
        public const int VtblSize = 3 + 1;
#endif
        void Invoke();
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IInvokeProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Invoke(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IInvokeProvider>((ComWrappers.ComInterfaceDispatch*)@this).Invoke();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IInvokeProviderNativeWrapper : IInvokeProvider
    {
        public static void Invoke(void* @this) => AutomationNodeWrapper.Invoke(@this, 3);

        void IInvokeProvider.Invoke() => Invoke(((AutomationNodeWrapper)this).IInvokeProviderInst);
    }
#endif
}
