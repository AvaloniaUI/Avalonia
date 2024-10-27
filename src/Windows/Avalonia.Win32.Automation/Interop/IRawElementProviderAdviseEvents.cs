using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("a407b27b-0f6d-4427-9292-473c7bf93258")]
internal partial interface IRawElementProviderAdviseEvents
{
    void AdviseEventAdded(int eventId,
#if NET8_0_OR_GREATER
        [MarshalUsing(typeof(SafeArrayMarshaller<int>))]
#endif
        int[] properties);

    void AdviseEventRemoved(int eventId,
#if NET8_0_OR_GREATER
        [MarshalUsing(typeof(SafeArrayMarshaller<int>))]
#endif
        int[] properties);
}
