using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("a407b27b-0f6d-4427-9292-473c7bf93258")]
internal partial interface IRawElementProviderAdviseEvents
{
    void AdviseEventAdded(int eventId,
        [MarshalUsing(typeof(SafeArrayMarshaller<int>))]
        int[] properties);

    void AdviseEventRemoved(int eventId,
        [MarshalUsing(typeof(SafeArrayMarshaller<int>))]
        int[] properties);
}
