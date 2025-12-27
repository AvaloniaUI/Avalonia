///////////////////////////////////////////////////////////////////////////////
//
//	PURPOSE
//		Wintab data management for WintabDN
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
    /// <summary>
    /// Wintab Packet bits.
    /// </summary>
    public enum EWintabPacketBit
    {
        PK_CONTEXT = 0x0001, /* reporting context */
        PK_STATUS = 0x0002, /* status bits */
        PK_TIME = 0x0004, /* time stamp */
        PK_CHANGED = 0x0008, /* change bit vector */
        PK_SERIAL_NUMBER = 0x0010, /* packet serial number */
        PK_CURSOR = 0x0020, /* reporting cursor */
        PK_BUTTONS = 0x0040, /* button information */
        PK_X = 0x0080, /* x axis */
        PK_Y = 0x0100, /* y axis */
        PK_Z = 0x0200, /* z axis */
        PK_NORMAL_PRESSURE = 0x0400, /* normal or tip pressure */
        PK_TANGENT_PRESSURE = 0x0800, /* tangential or barrel pressure */
        PK_ORIENTATION = 0x1000, /* orientation info: tilts */
        PK_PKTBITS_ALL = 0x1FFF // The Full Monty - all the bits execept Rotation - not supported
    }

    /// <summary>
    /// Wintab event messsages sent to an application.
    /// See Wintab Spec 1.4 for a description of these messages.
    /// </summary>
    public enum EWintabEventMessage
    {
        WT_PACKET = 0x7FF0,
        WT_CTXOPEN = 0x7FF1,
        WT_CTXCLOSE = 0x7FF2,
        WT_CTXUPDATE = 0x7FF3,
        WT_CTXOVERLAP = 0x7FF4,
        WT_PROXIMITY = 0x7FF5,
        WT_INFOCHANGE = 0x7FF6,
        WT_CSRCHANGE = 0x7FF7,
        WT_PACKETEXT = 0x7FF8
    }

    /// <summary>
    /// Wintab packet status values.
    /// </summary>
    public enum EWintabPacketStatusValue
    {
        /// <summary>
        /// Specifies that the cursor is out of the context.
        /// </summary>
        TPS_PROXIMITY = 0x0001,

        /// <summary>
        /// Specifies that the event queue for the context has overflowed.
        /// </summary>
        TPS_QUEUE_ERR = 0x0002,

        /// <summary>
        /// Specifies that the cursor is in the margin of the context.
        /// </summary>
        TPS_MARGIN = 0x0004,

        /// <summary>
        /// Specifies that the cursor is out of the context, but that the
        /// context has grabbed input while waiting for a button release event.
        /// </summary>
        TPS_GRAB = 0x0008,

        /// <summary>
        /// Specifies that the cursor is in its inverted state.
        /// </summary>
        TPS_INVERT = 0x0010
    }

    /// <summary>
    /// WintabPacket.pkButton codes.
    /// </summary>
    public enum EWintabPacketButtonCode
    {
        /// <summary>
        /// No change in button state.
        /// </summary>
        TBN_NONE = 0,

        /// <summary>
        /// Button was released.
        /// </summary>
        TBN_UP = 1,

        /// <summary>
        /// Button was pressed.
        /// </summary>
        TBN_DOWN = 2
    }

    /// <summary>
    /// Pen Orientation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTOrientation
    {
        /// <summary>
        /// Specifies the clockwise rotation of the cursor about the
        /// z axis through a full circular range.
        /// </summary>
        public Int32 orAzimuth;

        /// <summary>
        /// Specifies the angle with the x-y plane through a signed, semicircular range.
        /// Positive values specify an angle upward toward the positive z axis; negative
        /// values specify an angle downward toward the negative z axis.
        /// </summary>
        public Int32 orAltitude;

        /// <summary>
        /// Specifies the clockwise rotation of the cursor about its own major axis.
        /// </summary>
        public Int32 orTwist;
    }

    /// <summary>
    /// Pen Rotation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTRotation
    {
        /// <summary>
        /// Specifies the pitch of the cursor.
        /// </summary>
        public Int32 rotPitch;

        /// <summary>
        /// Specifies the roll of the cursor.
        /// </summary>
        public Int32 rotRoll;

        /// <summary>
        /// Specifies the yaw of the cursor.
        /// </summary>
        public Int32 rotYaw;
    }

    /// <summary>
    /// Wintab data packet.  Contains the "Full Monty" for all possible data values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WintabPacket
    {
        /// <summary>
        /// Specifies the context that generated the event.
        /// </summary>
        public HCTX pkContext; // PK_CONTEXT

        /// <summary>
        /// Specifies various status and error conditions. These conditions can be
        /// combined by using the bitwise OR opera-tor. The pkStatus field can be any
        /// any combination of the values defined in EWintabPacketStatusValue.
        /// </summary>
        public UInt32 pkStatus; // PK_STATUS

        /// <summary>
        /// In absolute mode, specifies the system time at which the event was posted. In
        /// relative mode, specifies the elapsed time in milliseconds since the last packet.
        /// </summary>
        public UInt32 pkTime; // PK_TIME

        /// <summary>
        /// Specifies which of the included packet data items have changed since the
        /// previously posted event.
        /// </summary>
        public WTPKT pkChanged; // PK_CHANGED

        /// <summary>
        /// This is an identifier assigned to the packet by the context. Consecutive
        /// packets will have consecutive serial numbers.
        /// </summary>
        public UInt32 pkSerialNumber; // PK_SERIAL_NUMBER

        /// <summary>
        /// Specifies which cursor type generated the packet.
        /// </summary>
        public UInt32 pkCursor; // PK_CURSOR

        /// <summary>
        /// In absolute mode, is a UInt32 containing the current button state.
        /// In relative mode, is a UInt32 whose low word contains a button number,
        /// and whose high word contains one of the codes in EWintabPacketButtonCode.
        /// </summary>
        public UInt32 pkButtons; // PK_BUTTONS

        /// <summary>
        /// In absolute mode, each is a UInt32 containing the scaled cursor location
        /// along the X axis.  In relative mode, this is an Int32 containing
        /// scaled change in cursor position.
        /// </summary>
        public Int32 pkX; // PK_X

        /// <summary>
        /// In absolute mode, each is a UInt32 containing the scaled cursor location
        /// along the Y axis.  In relative mode, this is an Int32 containing
        /// scaled change in cursor position.
        /// </summary>
        public Int32 pkY; // PK_Y

        /// <summary>
        /// In absolute mode, each is a UInt32 containing the scaled cursor location
        /// along the Z axis.  In relative mode, this is an Int32 containing
        /// scaled change in cursor position.
        /// </summary>
        public Int32 pkZ; // PK_Z

        /// <summary>
        /// In absolute mode, this is a UINT containing the adjusted state
        /// of the normal pressure, respectively. In relative mode, this is
        /// an int containing the change in adjusted pressure state.
        /// </summary>
        public UInt32 pkNormalPressure; // PK_NORMAL_PRESSURE

        /// <summary>
        /// In absolute mode, this is a UINT containing the adjusted state
        /// of the tangential pressure, respectively. In relative mode, this is
        /// an int containing the change in adjusted pressure state.
        /// </summary>
        public UInt32 pkTangentPressure; // PK_TANGENT_PRESSURE

        /// <summary>
        /// Contains updated cursor orientation information. See the
        /// WTOrientation structure for details.
        /// </summary>
        public WTOrientation pkOrientation; // ORIENTATION
    }

    /// <summary>
    /// Common properties for control extension data transactions.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTExtensionBase
    {
        /// <summary>
        /// Specifies the Wintab context to which these properties apply.
        /// </summary>
        public HCTX nContext;

        /// <summary>
        /// Status of setting/getting properties.
        /// </summary>
        public UInt32 nStatus;

        /// <summary>
        /// Timestamp applied to property transaction.
        /// </summary>
        public WTPKT nTime;

        /// <summary>
        /// Reserved - not used.
        /// </summary>
        public UInt32 nSerialNumber;
    }

    /// <summary>
    /// Extension data for one Express Key.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTExpKeyData
    {
        /// <summary>
        /// Tablet index where control is found.
        /// </summary>
        public byte nTablet;

        /// <summary>
        /// Zero-based control index.
        /// </summary>
        public byte nControl;

        /// <summary>
        /// Zero-based index indicating side of tablet where control found (0 = left, 1 = right).
        /// </summary>
        public byte nLocation;

        /// <summary>
        /// Reserved - not used
        /// </summary>
        public byte nReserved;

        /// <summary>
        /// Indicates Express Key button press (1 = pressed, 0 = released)
        /// </summary>
        public WTPKT nState;
    }

    /// <summary>
    /// Extension data for one touch ring or one touch strip.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTSliderData
    {
        /// <summary>
        /// Tablet index where control is found.
        /// </summary>
        public byte nTablet;

        /// <summary>
        /// Zero-based control index.
        /// </summary>
        public byte nControl;

        /// <summary>
        /// Zero-based current active mode of control.
        /// This is the mode selected by control's toggle button.
        /// </summary>
        public byte nMode;

        /// <summary>
        /// Reserved - not used
        /// </summary>
        public byte nReserved;

        /// <summary>
        /// An integer representing the position of the user's finger on the control.
        /// When there is no finger on the control, this value is negative.
        /// </summary>
        public WTPKT nPosition;
    }

    /// <summary>
    /// Wintab extension data packet for one tablet control.
    /// The tablet controls for which extension data is available are: Express Key, Touch Ring and Touch Strip controls.
    /// Note that tablets will have either Touch Rings or Touch Strips - not both.
    /// All tablets have Express Keys.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WintabPacketExt
    {
        /// <summary>
        /// Extension control properties common to all control types.
        /// </summary>
        public WTExtensionBase pkBase;

        /// <summary>
        /// Extension data for one Express Key.
        /// </summary>
        public WTExpKeyData pkExpKey;

        /// <summary>
        /// Extension data for one Touch Strip.
        /// </summary>
        public WTSliderData pkTouchStrip;

        /// <summary>
        /// Extension data for one Touch Ring.
        /// </summary>
        public WTSliderData pkTouchRing;
    }

    /// <summary>
    /// Class to support capture and management of Wintab daa.
    /// </summary>
    public class WintabData
    {
        private WintabContext m_context;

        /// <summary>
        /// CWintabData constructor
        /// </summary>
        /// <param name="context_I">logical context for this data object</param>
        public WintabData(WintabContext context_I)
        {
            m_context = context_I;
        }

        /*
        /// <summary>
        /// Removes the WTPacket handler so that messages are ignored.
        /// </summary>
        /// <param name="handler_I">WT_PACKET event handler supplied by the client.</param>
        public void RemoveWTPacketEventHandler(EventHandler<MessageReceivedEventArgs> handler_I)
        {
            try
            {
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_PACKET);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_PACKETEXT);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_CSRCHANGE);
                MessageEvents.PacketMessageReceived -= handler_I;
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED CWintabData.RemoveWTPacketEventHandler: " + ex.ToString());
            }
        }

        /// <summary>
        /// Set the handler to be called when WT_CTX* messages are received.
        /// </summary>
        /// <param name="handler_I">WT_CTX* event handler supplied by the client.</param>
        public void SetStatusEventHandler(EventHandler<MessageReceivedEventArgs> handler_I)
        {
            try
            {
                MessageEvents.StatusMessageReceived += handler_I;
                MessageEvents.WatchMessage(EWintabEventMessage.WT_CTXOPEN);
                MessageEvents.WatchMessage(EWintabEventMessage.WT_CTXCLOSE);
                MessageEvents.WatchMessage(EWintabEventMessage.WT_CTXUPDATE);
                MessageEvents.WatchMessage(EWintabEventMessage.WT_CTXOVERLAP);
                MessageEvents.WatchMessage(EWintabEventMessage.WT_PROXIMITY);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED CWintabData.SetStatusEventHandler: " + ex.ToString());
            }
        }

        /// <summary>
        /// Removes the WT_CTX* handler so that messages are ignored.
        /// </summary>
        /// <param name="handler_I"> WT_CTX* event handler supplied by the client.</param>
        public void RemoveStatusEventHandler(EventHandler<MessageReceivedEventArgs> handler_I)
        {
            try
            {
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_CTXOPEN);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_CTXCLOSE);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_CTXUPDATE);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_CTXOVERLAP);
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_PROXIMITY);
                MessageEvents.StatusMessageReceived -= handler_I;
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED CWintabData.RemoveStatusEventHandler: " + ex.ToString());
            }
        }

        /// <summary>
        /// Set the handler to be called when WT_INFOCHANGE messages are received.
        /// </summary>
        /// <param name="handler_I">WT_INFOCHANGE event handler supplied by the client.</param>
        public void SetInfoChangeEventHandler(EventHandler<MessageReceivedEventArgs> handler_I)
        {
            try
            {
                MessageEvents.InfoChgMessageReceived += handler_I;
                MessageEvents.WatchMessage(EWintabEventMessage.WT_INFOCHANGE);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED CWintabData.SetWTInfoChangeEventHandler: " + ex.ToString());
            }
        }

        /// <summary>
        /// Removes the WT_INFOCHANGE handler so that messages are ignored.
        /// </summary>
        /// <param name="handler_I">WT_INFOCHANGE event handler supplied by the client.</param>
        public void RemoveInfoChangeEventHandler(EventHandler<MessageReceivedEventArgs> handler_I)
        {
            try
            {
                MessageEvents.UnWatchMessage(EWintabEventMessage.WT_INFOCHANGE);
                MessageEvents.InfoChgMessageReceived -= handler_I;
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED CWintabData.RemoveWTInfoChangeEventHandler: " + ex.ToString());
            }
        }
        */

        /// <summary>
        /// Set packet queue size for this data object's context.
        /// </summary>
        /// <param name="numPkts_I">desired #packets in queue</param>
        /// <returns>Returns true if operation successful</returns>
        public bool SetPacketQueueSize(UInt32 numPkts_I)
        {
            bool status = false;


            CheckForValidHCTX("SetPacketQueueSize");
            status = WintabFuncs.WTQueueSizeSet(m_context.HCtx, numPkts_I);

            return status;
        }

        /// <summary>
        /// Get packet queue size for this data object's context.
        /// </summary>
        /// <returns>Returns a packet queue size in #packets or 0 if fails</returns>
        public UInt32 GetPacketQueueSize()
        {
            UInt32 numPkts = 0;

            CheckForValidHCTX("GetPacketQueueSize");
            numPkts = WintabFuncs.WTQueueSizeGet(m_context.HCtx);

            return numPkts;
        }

        public void BringToFront()
        {
            CheckForValidHCTX("BringToFront");
            WintabFuncs.WTOverlap(m_context.HCtx, true);
        }

        /// <summary>
        /// Returns one packet of WintabPacketExt data from the packet queue.
        /// </summary>
        /// <param name="hCtx_I">Wintab context to be used when asking for the data</param>
        /// <param name="pktID_I">Identifier for the tablet event packet to return.</param>
        /// <returns>Returns a data packet with non-null context if successful.</returns>
        public WintabPacketExt GetDataPacketExt(UInt32 hCtx_I, UInt32 pktID_I)
        {
            int size = (int)(Marshal.SizeOf(new WintabPacketExt()));
            IntPtr buf = Marshal.AllocHGlobal(size);
            WintabPacketExt[] packets = [];

            bool status = false;

            if (pktID_I == 0)
            {
                throw new Exception("GetDataPacket - invalid pktID");
            }

            CheckForValidHCTX("GetDataPacket");
            status = WintabFuncs.WTPacket(hCtx_I, pktID_I, buf);

            if (status)
            {
                packets = WintabMemUtils.MarshalDataExtPackets(1, buf);
            }
            else
            {
                // If fails, make sure context is zero.
                packets[0].pkBase.nContext = 0;
            }

            return packets[0];
        }

        /// <summary>
        /// Returns one packet of WintabPacket data from the packet queue. (Deprecated)
        /// </summary>
        /// <param name="pktID_I">Identifier for the tablet event packet to return.</param>
        /// <returns>Returns a data packet with non-null context if successful.</returns>
        public WintabPacket GetDataPacket(UInt32 pktID_I)
        {
            return GetDataPacket(m_context.HCtx, pktID_I);
        }

        /// <summary>
        /// Returns one packet of Wintab data from the packet queue.
        /// </summary>
        /// <param name="hCtx_I">Wintab context to be used when asking for the data</param>
        /// <param name="pktID_I">Identifier for the tablet event packet to return.</param>
        /// <returns>Returns a data packet with non-null context if successful.</returns>
        public WintabPacket GetDataPacket(UInt32 hCtx_I, UInt32 pktID_I)
        {
            IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf<WintabPacket>());
            WintabPacket packet = new WintabPacket();

            if (pktID_I == 0)
            {
                throw new Exception("GetDataPacket - invalid pktID");
            }

            CheckForValidHCTX("GetDataPacket");

            if (WintabFuncs.WTPacket(hCtx_I, pktID_I, buf))
            {
                packet = Marshal.PtrToStructure<WintabPacket>(buf);
            }
            else
            {
                //
                // If fails, make sure context is zero.
                //
                packet.pkContext = 0;
            }

            return packet;
        }

        /// <summary>
        /// Removes all pending data packets from the context's queue.
        /// </summary>
        public void FlushDataPackets(uint numPacketsToFlush_I)
        {
            CheckForValidHCTX("FlushDataPackets");
            WintabFuncs.WTPacketsGet(m_context.HCtx, numPacketsToFlush_I, IntPtr.Zero);
        }

        /// <summary>
        /// Returns an array of Wintab data packets from the packet queue.
        /// </summary>
        /// <param name="maxPkts_I">Specifies the maximum number of packets to return.</param>
        /// <param name="remove_I">If true, returns data packets and removes them from the queue.</param>
        /// <param name="numPkts_O">Number of packets actually returned.</param>
        /// <returns>Returns the next maxPkts_I from the list.  Note that if remove_I is false, then
        /// repeated calls will return the same packets.  If remove_I is true, then packets will be
        /// removed and subsequent calls will get different packets (if any).</returns>
        public WintabPacket[] GetDataPackets(UInt32 maxPkts_I, bool remove_I, ref UInt32 numPkts_O)
        {
            WintabPacket[] packets = [];

            CheckForValidHCTX("GetDataPackets");

            if (maxPkts_I == 0)
            {
                throw new Exception("GetDataPackets - maxPkts_I is zero.");
            }

            // Packet array is used whether we're just looking or buying.
            int size = (int)(maxPkts_I * Marshal.SizeOf(new WintabPacket()));
            IntPtr buf = Marshal.AllocHGlobal(size);

            if (remove_I)
            {
                // Return data packets and remove packets from queue.
                numPkts_O = WintabFuncs.WTPacketsGet(m_context.HCtx, maxPkts_I, buf);

                if (numPkts_O > 0)
                {
                    packets = WintabMemUtils.MarshalDataPackets(numPkts_O, buf);
                }

                //System.Diagnostics.Debug.WriteLine("GetDataPackets: numPkts_O: " + numPkts_O);
            }
            else
            {
                // Return data packets, but leave on queue.  (Peek mode)
                UInt32 pktIDOldest = 0;
                UInt32 pktIDNewest = 0;

                // Get oldest and newest packet identifiers in the queue.  These will bound the
                // packets that are actually returned.
                if (WintabFuncs.WTQueuePacketsEx(m_context.HCtx, ref pktIDOldest, ref pktIDNewest))
                {
                    UInt32 pktIDStart = pktIDOldest;
                    UInt32 pktIDEnd = pktIDNewest;

                    if (pktIDStart == 0)
                    {
                        throw new Exception("WTQueuePacketsEx reports zero start packet identifier");
                    }

                    if (pktIDEnd == 0)
                    {
                        throw new Exception("WTQueuePacketsEx reports zero end packet identifier");
                    }

                    // Peek up to the max number of packets specified.
                    UInt32 numFoundPkts = WintabFuncs.WTDataPeek(m_context.HCtx, pktIDStart, pktIDEnd, maxPkts_I, buf,
                        ref numPkts_O);

                    System.Diagnostics.Debug.WriteLine("GetDataPackets: WTDataPeek - numFoundPkts: " + numFoundPkts +
                                                       ", numPkts_O: " + numPkts_O);

                    if (numFoundPkts > 0 && numFoundPkts < numPkts_O)
                    {
                        throw new Exception("WTDataPeek reports more packets returned than actually exist in queue.");
                    }

                    packets = WintabMemUtils.MarshalDataPackets(numPkts_O, buf);
                }
            }

            return packets;
        }

        /// <summary>
        /// Throws exception if logical context for this data object is zero.
        /// </summary>
        private void CheckForValidHCTX(string msg)
        {
            if (m_context.HCtx == 0)
            {
                throw new Exception(msg + " - Bad Context");
            }
        }
    }
}
