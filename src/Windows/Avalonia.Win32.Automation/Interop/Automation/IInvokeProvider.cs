// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Interop.Automation;

#if NET8_0_OR_GREATER
[GeneratedComInterface]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
internal partial interface IInvokeProvider
{
    void Invoke();
}