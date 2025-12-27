///////////////////////////////////////////////////////////////////////////////
//
//	PURPOSE
//		Wintab32 function wrappers for WintabDN
//
//	COPYRIGHT
//		Copyright (c) 2010-2020 Wacom Co., Ltd.
//
//		The text and information contained in this file may be freely used,
//		copied, or distributed without compensation or licensing restrictions.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.WintabImpl
{
    using P_WTPKT = UInt32;
    using P_FIX32 = UInt32;
    using P_HCTX = UInt32;
    using P_HWND = System.IntPtr;

    //Implementation note: cannot use statement such as:
    //      using WTPKT = UInt32;
    // because the scope of the statement is this file only.
    // Thus we need to implement the 'typedef' using a class that
    // implicitly defines the type.  Also remember to make it
    // sequential so it won't make marshalling barf.

    /// <summary>
    /// Managed implementation of Wintab HWND typedef.
    /// Holds native Window handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct HWND
    {
        [MarshalAs(UnmanagedType.I4)] public IntPtr value;

        public HWND(IntPtr value)
        {
            this.value = value;
        }

        public static implicit operator IntPtr(HWND hwnd_I)
        {
            return hwnd_I.value;
        }

        public static implicit operator HWND(IntPtr ptr_I)
        {
            return new HWND(ptr_I);
        }

        public static bool operator ==(HWND hwnd1, HWND hwnd2)
        {
            return hwnd1.value == hwnd2.value;
        }

        public static bool operator !=(HWND hwnd1, HWND hwnd2)
        {
            return hwnd1.value != hwnd2.value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(HWND))
                return false;

            return (HWND)obj == this;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    /// <summary>
    /// Managed implementation of Wintab WTPKT typedef.
    /// Holds Wintab packet identifier.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class WTPKT
    {
        [MarshalAs(UnmanagedType.U4)] UInt32 value;

        public WTPKT(UInt32 value)
        {
            this.value = value;
        }

        public static implicit operator UInt32(WTPKT pkt_I)
        {
            return pkt_I.value;
        }

        public static implicit operator WTPKT(UInt32 value)
        {
            return new WTPKT(value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Managed implementation of Wintab FIX32 typedef.
    /// Used for a fixed-point arithmetic value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class FIX32
    {
        [MarshalAs(UnmanagedType.U4)] UInt32 value;

        public FIX32(UInt32 value)
        {
            this.value = value;
        }

        public static implicit operator UInt32(FIX32 fix32_I)
        {
            return fix32_I.value;
        }

        public static implicit operator FIX32(UInt32 value)
        {
            return new FIX32(value);
        }

        public double FixToDouble()
        {
            uint x = this;
            ushort integerPart = (ushort)(x >> 16);

            ushort fractionalPart = (ushort)(x & 0xFFFF);

            return integerPart + (fractionalPart / 65536.0);
        }

        public static double Frac(double x)
        {
            return x - Math.Floor(x);
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Managed implementation of Wintab HCTX typedef.
    /// Holds a Wintab context identifier.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class HCTX
    {
        IntPtr value;

        public HCTX(UInt32 value)
        {
            this.value = new IntPtr(value);
        }

        public static implicit operator UInt32(HCTX hctx_I)
        {
            return (UInt32)hctx_I.value.ToInt32();
        }

        public static implicit operator HCTX(UInt32 value)
        {
            return new HCTX(value);
        }

        public static bool operator ==(HCTX hctx, UInt32 value)
        {
            return (UInt32)hctx.value.ToInt32() == value;
        }

        public static bool operator !=(HCTX hctx, UInt32 value)
        {
            return (UInt32)hctx.value.ToInt32() != value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(HCTX))
                return false;

            return (HCTX)obj == this;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Index values for WTInfo wCategory parameter.
    /// </summary>
    public enum EWTICategoryIndex
    {
        WTI_INTERFACE = 1,
        WTI_STATUS = 2,
        WTI_DEFCONTEXT = 3,
        WTI_DEFSYSCTX = 4,
        WTI_DEVICES = 100,
        WTI_CURSORS = 200,
        WTI_EXTENSIONS = 300,
        WTI_DDCTXS = 400,
        WTI_DSCTXS = 500
    }

    /// <summary>
    /// Index values for WTI_INTERFACE.
    /// </summary>
    public enum EWTIInterfaceIndex
    {
        IFC_WINTABID = 1,
        IFC_SPECVERSION = 2,
        IFC_IMPLVERSION = 3,
        IFC_NDEVICES = 4,
        IFC_NCURSORS = 5,
        IFC_NCONTEXTS = 6,
        IFC_CTXOPTIONS = 7,
        IFC_CTXSAVESIZE = 8,
        IFC_NEXTENSIONS = 9,
        IFC_NMANAGERS = 10
    }

    /// <summary>
    /// Index values for WTI_DEVICES
    /// </summary>
    public enum EWTIDevicesIndex
    {
        DVC_NAME = 1,
        DVC_HARDWARE = 2,
        DVC_NCSRTYPES = 3,
        DVC_FIRSTCSR = 4,
        DVC_PKTRATE = 5,
        DVC_PKTDATA = 6,
        DVC_PKTMODE = 7,
        DVC_CSRDATA = 8,
        DVC_XMARGIN = 9,
        DVC_YMARGIN = 10,
        DVC_ZMARGIN = 11,
        DVC_X = 12,
        DVC_Y = 13,
        DVC_Z = 14,
        DVC_NPRESSURE = 15,
        DVC_TPRESSURE = 16,
        DVC_ORIENTATION = 17,
        DVC_ROTATION = 18,
        DVC_PNPID = 19
    }

    /// <summary>
    /// Index values for WTI_CURSORS.
    /// </summary>
    public enum EWTICursorsIndex
    {
        CSR_NAME = 1,
        CSR_ACTIVE = 2,
        CSR_PKTDATA = 3,
        CSR_BUTTONS = 4,
        CSR_BUTTONBITS = 5,
        CSR_BTNNAMES = 6,
        CSR_BUTTONMAP = 7,
        CSR_SYSBTNMAP = 8,
        CSR_NPBUTTON = 9,
        CSR_NPBTNMARKS = 10,
        CSR_NPRESPONSE = 11,
        CSR_TPBUTTON = 12,
        CSR_TPBTNMARKS = 13,
        CSR_TPRESPONSE = 14,
        CSR_PHYSID = 15,
        CSR_MODE = 16,
        CSR_MINPKTDATA = 17,
        CSR_MINBUTTONS = 18,
        CSR_CAPABILITIES = 19,
        CSR_TYPE = 20
    }

    /// <summary>
    /// Index used with CSR_NAME to get stylus types.
    /// </summary>
    public enum EWTICursorNameIndex
    {
        CSR_NAME_PUCK = EWTICategoryIndex.WTI_CURSORS + 0,
        CSR_NAME_PRESSURE_STYLUS = EWTICategoryIndex.WTI_CURSORS + 1,
        CSR_NAME_ERASER = EWTICategoryIndex.WTI_CURSORS + 2
    }

    /// <summary>
    /// Index values for WTI contexts.
    /// </summary>
    public enum EWTIContextIndex
    {
        CTX_NAME = 1,
        CTX_OPTIONS = 2,
        CTX_STATUS = 3,
        CTX_LOCKS = 4,
        CTX_MSGBASE = 5,
        CTX_DEVICE = 6,
        CTX_PKTRATE = 7,
        CTX_PKTDATA = 8,
        CTX_PKTMODE = 9,
        CTX_MOVEMASK = 10,
        CTX_BTNDNMASK = 11,
        CTX_BTNUPMASK = 12,
        CTX_INORGX = 13,
        CTX_INORGY = 14,
        CTX_INORGZ = 15,
        CTX_INEXTX = 16,
        CTX_INEXTY = 17,
        CTX_INEXTZ = 18,
        CTX_OUTORGX = 19,
        CTX_OUTORGY = 20,
        CTX_OUTORGZ = 21,
        CTX_OUTEXTX = 22,
        CTX_OUTEXTY = 23,
        CTX_OUTEXTZ = 24,
        CTX_SENSX = 25,
        CTX_SENSY = 26,
        CTX_SENSZ = 27,
        CTX_SYSMODE = 28,
        CTX_SYSORGX = 29,
        CTX_SYSORGY = 30,
        CTX_SYSEXTX = 31,
        CTX_SYSEXTY = 32,
        CTX_SYSSENSX = 33,
        CTX_SYSSENSY = 34
    }

    /// <summary>
    /// P/Invoke wrappers for Wintab functions.
    /// See Wintab_v140.doc (Wintab 1.4 spec) and related Wintab documentation for details.
    /// </summary>
    public class WintabFuncs
    {
        /// <summary>
        /// This function returns global information about the interface in an application-supplied buffer.
        /// Different types of information are specified by different index arguments. Applications use this
        /// function to receive information about tablet coordinates, physical dimensions, capabilities, and
        /// cursor types.
        /// </summary>
        /// <param name="wCategory_I">Identifies the category from which information is being requested.</param>
        /// <param name="nIndex_I">Identifies which information is being requested from within the category.</param>
        /// <param name="lpOutput_O">Points to a buffer to hold the requested information.</param>
        /// <returns>The return value specifies the size of the returned information in bytes. If the information
        /// is not supported, the function returns zero. If a tablet is not physically present, this function
        /// always returns zero.
        /// </returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 WTInfoA(UInt32 wCategory_I, UInt32 nIndex_I, IntPtr lpOutput_O);

        /// <summary>
        /// This function establishes an active context on the tablet. On successful completion of this function,
        /// the application may begin receiving tablet events via messages (if they were requested), and may use
        /// the handle returned to poll the context, or to perform other context-related functions.
        /// </summary>
        /// <param name="hWnd_I">Identifies the window that owns the tablet context, and receives messages from the context.</param>
        /// <param name="logContext_I">Points to an application-provided WintabLogContext data structure describing the context to be opened.</param>
        /// <param name="enable_I">Specifies whether the new context will immediately begin processing input data.</param>
        /// <returns>The return value identifies the new context. It is NULL if the context is not opened.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern P_HCTX WTOpenA(P_HWND hWnd_I, ref WintabLogContext logContext_I, bool enable_I);

        /// <summary>
        /// This function closes and destroys the tablet context object.
        /// </summary>
        /// <param name="hctx_I">Identifies the context to be closed.</param>
        /// <returns>The function returns a non-zero value if the context was valid and was destroyed. Otherwise, it returns zero.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTClose(P_HCTX hctx_I);

        /// <summary>
        /// This function enables or disables a tablet context, temporarily turning on or off the processing of packets.
        /// </summary>
        /// <param name="hctx_I">Identifies the context to be enabled or disabled.</param>
        /// <param name="enable_I">Specifies enabling if non-zero, disabling if zero.</param>
        /// <returns>The function returns true if the enable or disable request was satisfied.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTEnable(P_HCTX hctx_I, bool enable_I);

        /// <summary>
        /// This function sends a tablet context to the top or bottom of the order of overlapping tablet contexts.
        /// </summary>
        /// <param name="hctx_I">Identifies the context to move within the overlap order.</param>
        /// <param name="toTop_I">Specifies sending the context to the top of the overlap order true, or to the bottom if false.</param>
        /// <returns>The function returns true if successful.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTOverlap(P_HCTX hctx_I, bool toTop_I);

        /// <summary>
        /// This function returns the number of packets the context's queue can hold.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose queue size is being returned.</param>
        /// <returns>The number of packets the queue can hold.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 WTQueueSizeGet(P_HCTX hctx_I);

        /// <summary>
        /// This function attempts to change the context's queue size to the value specified in nPkts_I.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose queue size is being set.</param>
        /// <param name="nPkts_I">Specifies the requested queue size.</param>
        /// <returns>The return value is true if the queue size was successfully changed.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTQueueSizeSet(P_HCTX hctx_I, UInt32 nPkts_I);

        /// <summary>
        /// This function fills in the passed pktBuf_O buffer with the context event packet having
        /// the specified serial number. The returned packet and any older packets are removed from
        /// the context's internal queue.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose packets are being returned.</param>
        /// <param name="pktSerialNum_I">Serial number of the tablet event to return.</param>
        /// <param name="pktBuf_O">Buffer to receive the event packet.</param>
        /// <returns>The return value is true if the specified packet was found and returned.
        /// It is false if the specified packet was not found in the queue.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTPacket(P_HCTX hctx_I, UInt32 pktSerialNum_I, IntPtr pktBuf_O);

        /// <summary>
        /// This function copies the next maxPkts_I events from the packet queue of context hCtx to
        /// the passed pktBuf_O buffer and removes them from the queue
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose packets are being returned.</param>
        /// <param name="maxPkts_I">Specifies the maximum number of packets to return</param>
        /// <param name="pktBuf_O">Buffer to receive the event packets.</param>
        /// <returns>The return value is the number of packets copied in the buffer.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 WTPacketsGet(P_HCTX hctx_I, UInt32 maxPkts_I, IntPtr pktBuf_O);

        /// <summary>A
        /// This function copies all packets with Identifiers between pktIDStart_I and pktIDEnd_I
        /// inclusive from the context's queue to the passed buffer and removes them from the queue.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose packets are being returned.</param>
        /// <param name="pktIDStart_I">Identifier of the oldest tablet event to return.</param>
        /// <param name="pktIDEnd_I">Identifier of the newest tablet event to return.</param>
        /// <param name="maxPkts_I">Specifies the maximum number of packets to return.</param>
        /// <param name="pktBuf_O">Buffer to receive the event packets.</param>
        /// <param name="numPkts_O">Number of packets actually copied.</param>
        /// <returns>The return value is the total number of packets found in the queue
        /// between pktIDStart_I and pktIDEnd_I.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 WTDataGet(P_HCTX hctx_I, UInt32 pktIDStart_I, UInt32 pktIDEnd_I,
            UInt32 maxPkts_I, IntPtr pktBuf_O, ref UInt32 numPkts_O);

        /// <summary>
        /// This function copies all packets with serial numbers between pktIDStart_I and pktIDEnd_I
        /// inclusive, from the context's queue to the passed buffer without removing them from the queue.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose packets are being read.</param>
        /// <param name="pktIDStart_I">Identifier of the oldest tablet event to return.</param>
        /// <param name="pktIDEnd_I">Identifier of the newest tablet event to return.</param>
        /// <param name="maxPkts_I">Specifies the maximum number of packets to return.</param>
        /// <param name="pktBuf_O">Buffer to receive the event packets.</param>
        /// <param name="numPkts_O">Number of packets actually copied.</param>
        /// <returns>The return value is the total number of packets found in the queue between
        /// pktIDStart_I and pktIDEnd_I.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 WTDataPeek(P_HCTX hctx_I, UInt32 pktIDStart_I, UInt32 pktIDEnd_I,
            UInt32 maxPkts_I, IntPtr pktBuf_O, ref UInt32 numPkts_O);

        /// <summary>
        /// This function returns the identifiers of the oldest and newest packets currently in the queue.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose queue is being queried.</param>
        /// <param name="pktIDOldest_O">Identifier of the oldest packet in the queue.</param>
        /// <param name="pktIDNewest_O">Identifier of the newest packet in the queue.</param>
        /// <returns>This function returns bool if successful.</returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTQueuePacketsEx(P_HCTX hctx_I, ref UInt32 pktIDOldest_O, ref UInt32 pktIDNewest_O);

        /// <summary>
        /// This function retrieves any context-specific data for an extension.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose extension attributes are being retrieved.</param>
        /// <param name="extTag_I">Identifies the extension tag for which context-specific data is being retrieved.</param>
        /// <param name="extData_O">Points to a buffer to hold retrieved data (WTExtensionProperty).</param>
        /// <returns></returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTExtGet(P_HCTX hctx_I, UInt32 extTag_I, IntPtr extData_O);

        /// <summary>
        /// This function sets any context-specific data for an extension.
        /// </summary>
        /// <param name="hctx_I">Identifies the context whose extension attributes are being modified.</param>
        /// <param name="extTag_I">Identifies the extension tag for which context-specific data is being modified.</param>
        /// <param name="extData_I">Points to the new data (WTExtensionProperty).</param>
        /// <returns></returns>
        [DllImport("Wintab32.dll", CharSet = CharSet.Auto)]
        public static extern bool WTExtSet(P_HCTX hctx_I, UInt32 extTag_I, IntPtr extData_I);
    }
}
